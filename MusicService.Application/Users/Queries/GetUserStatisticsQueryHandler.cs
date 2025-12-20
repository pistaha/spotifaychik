using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Users.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Users.Queries
{
    public class GetUserStatisticsQueryHandler : IRequestHandler<GetUserStatisticsQuery, UserStatisticsDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IListenHistoryRepository _listenHistoryRepository;
        private readonly IArtistRepository _artistRepository;
        private readonly ITrackRepository _trackRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUserStatisticsQueryHandler> _logger;

        public GetUserStatisticsQueryHandler(
            IUserRepository userRepository,
            IPlaylistRepository playlistRepository,
            IListenHistoryRepository listenHistoryRepository,
            IArtistRepository artistRepository,
            ITrackRepository trackRepository,
            IMapper mapper,
            ILogger<GetUserStatisticsQueryHandler> logger)
        {
            _userRepository = userRepository;
            _playlistRepository = playlistRepository;
            _listenHistoryRepository = listenHistoryRepository;
            _artistRepository = artistRepository;
            _trackRepository = trackRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserStatisticsDto> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting statistics for user {UserId}", request.UserId);

            var statistics = new UserStatisticsDto();

            try
            {
                // Получаем пользователя
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", request.UserId);
                    return statistics;
                }

                // Получаем плейлисты пользователя
                var playlists = await _playlistRepository.GetUserPlaylistsAsync(request.UserId, cancellationToken);
                statistics.TotalPlaylists = playlists.Count;

                // Получаем историю прослушиваний
                var history = await _listenHistoryRepository.GetUserHistoryAsync(
                    request.UserId,
                    request.TimeRangeDays.HasValue ? DateTime.UtcNow.AddDays(-request.TimeRangeDays.Value) : null,
                    null,
                    cancellationToken);

                if (history.Any())
                {
                    // Общее время прослушивания
                    statistics.TotalListeningTime = history.Sum(h => h.ListenDurationSeconds) / 60;

                    // Даты первого и последнего прослушивания
                    statistics.FirstListenDate = history.Min(h => h.ListenedAt);
                    statistics.LastListenDate = history.Max(h => h.ListenedAt);

                    // Топ треков
                    var trackStats = history
                        .GroupBy(h => h.TrackId)
                        .Select(g => new TrackStatisticsDto
                        {
                            TrackId = g.Key,
                            TrackTitle = g.First().Track?.Title ?? "Unknown",
                            ArtistName = g.First().Track?.Artist?.Name ?? "Unknown",
                            ListenCount = g.Count(),
                            LastListenDate = g.Max(h => h.ListenedAt)
                        })
                        .OrderByDescending(t => t.ListenCount)
                        .Take(10)
                        .ToList();

                    statistics.TopTracks = trackStats;

                    // Топ артистов (через треки)
                        var artistStats = history
                            .Where(h => h.Track?.ArtistId != null)
                            .GroupBy(h => h.Track!.ArtistId)
                            .Select(g => new ArtistStatisticsDto
                            {
                                ArtistId = g.Key,
                                ArtistName = g.First().Track?.Artist?.Name ?? "Unknown",
                                ListenCount = g.Count(),
                                TotalDuration = g.Sum(h => h.ListenDurationSeconds) / 60
                        })
                        .OrderByDescending(a => a.ListenCount)
                        .Take(10)
                        .ToList();

                    statistics.TopArtists = artistStats;

                    // Топ жанров (из артистов)
                    var allArtists = await _artistRepository.GetAllAsync(cancellationToken);
                    var genreCounts = new Dictionary<string, int>();

                    foreach (var artistStat in artistStats)
                    {
                        var artist = allArtists.FirstOrDefault(a => a.Id == artistStat.ArtistId);
                        if (artist != null)
                        {
                            foreach (var genre in artist.Genres)
                            {
                                if (genreCounts.ContainsKey(genre))
                                    genreCounts[genre] += artistStat.ListenCount;
                                else
                                    genreCounts[genre] = artistStat.ListenCount;
                            }
                        }
                    }

                    statistics.TopGenres = genreCounts
                        .OrderByDescending(g => g.Value)
                        .Take(5)
                        .Select(g => g.Key)
                        .ToList();

                    statistics.FavoriteGenresCount = statistics.TopGenres.Count;
                }

                // Количество друзей (подписчиков и подписок)
                var friends = await _userRepository.GetUserFriendsAsync(request.UserId, cancellationToken);
                statistics.FollowersCount = friends.Count;

                // Для FollowingCount нужно считать подписки на артистов и плейлисты
                // В упрощенной версии считаем только друзей
                statistics.FollowingCount = statistics.FollowersCount;

                _logger.LogInformation("Statistics retrieved successfully for user {UserId}", request.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for user {UserId}", request.UserId);
            }

            return statistics;
        }
    }
}
