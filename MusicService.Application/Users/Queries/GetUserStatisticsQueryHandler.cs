using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Users.Queries
{
    public class GetUserStatisticsQueryHandler : IRequestHandler<GetUserStatisticsQuery, UserStatisticsDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly ILogger<GetUserStatisticsQueryHandler> _logger;

        public GetUserStatisticsQueryHandler(
            IMusicServiceDbContext dbContext,
            ILogger<GetUserStatisticsQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserStatisticsDto> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting statistics for user {UserId}", request.UserId);

            var statistics = new UserStatisticsDto();

            try
            {
                var userExists = await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Id == request.UserId, cancellationToken);
                if (!userExists)
                {
                    _logger.LogWarning("User {UserId} not found", request.UserId);
                    return statistics;
                }

                statistics.TotalPlaylists = await _dbContext.Playlists
                    .AsNoTracking()
                    .CountAsync(p => p.CreatedById == request.UserId, cancellationToken);

                var historyQuery = _dbContext.ListenHistories
                    .AsNoTracking()
                    .Where(h => h.UserId == request.UserId);

                if (request.TimeRangeDays.HasValue)
                {
                    var cutoff = DateTime.UtcNow.AddDays(-request.TimeRangeDays.Value);
                    historyQuery = historyQuery.Where(h => h.ListenedAt >= cutoff);
                }

                var hasHistory = await historyQuery.AnyAsync(cancellationToken);
                if (hasHistory)
                {
                    statistics.TotalListeningTime = await historyQuery
                        .SumAsync(h => h.ListenDurationSeconds, cancellationToken) / 60;

                    statistics.FirstListenDate = await historyQuery
                        .MinAsync(h => h.ListenedAt, cancellationToken);
                    statistics.LastListenDate = await historyQuery
                        .MaxAsync(h => h.ListenedAt, cancellationToken);

                    var topTracks = await historyQuery
                        .GroupBy(h => h.TrackId)
                        .Select(g => new { TrackId = g.Key, ListenCount = g.Count(), LastListenDate = g.Max(h => h.ListenedAt) })
                        .OrderByDescending(t => t.ListenCount)
                        .Take(10)
                        .Join(_dbContext.Tracks.AsNoTracking().Include(t => t.Artist).AsSplitQuery(),
                            stats => stats.TrackId,
                            track => track.Id,
                            (stats, track) => new TrackStatisticsDto
                            {
                                TrackId = stats.TrackId,
                                TrackTitle = track.Title,
                                ArtistName = track.Artist != null ? track.Artist.Name : "Unknown",
                                ListenCount = stats.ListenCount,
                                LastListenDate = stats.LastListenDate
                            })
                        .ToListAsync(cancellationToken);

                    statistics.TopTracks = topTracks;

                    var topArtists = await historyQuery
                        .GroupBy(h => h.Track!.ArtistId)
                        .Select(g => new { ArtistId = g.Key, ListenCount = g.Count(), TotalDuration = g.Sum(h => h.ListenDurationSeconds) / 60 })
                        .OrderByDescending(a => a.ListenCount)
                        .Take(10)
                        .Join(_dbContext.Artists.AsNoTracking(),
                            stats => stats.ArtistId,
                            artist => artist.Id,
                            (stats, artist) => new ArtistStatisticsDto
                            {
                                ArtistId = artist.Id,
                                ArtistName = artist.Name,
                                ListenCount = stats.ListenCount,
                                TotalDuration = stats.TotalDuration
                            })
                        .ToListAsync(cancellationToken);

                    statistics.TopArtists = topArtists;

                    var artistIds = topArtists.Select(a => a.ArtistId).ToList();
                    var artistsWithGenres = await _dbContext.Artists
                        .AsNoTracking()
                        .Where(a => artistIds.Contains(a.Id))
                        .Select(a => new { a.Id, a.Genres })
                        .ToListAsync(cancellationToken);

                    var genreCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var artistStat in topArtists)
                    {
                        var artistGenres = artistsWithGenres.FirstOrDefault(a => a.Id == artistStat.ArtistId)?.Genres;
                        if (artistGenres == null)
                        {
                            continue;
                        }

                        foreach (var genre in artistGenres)
                        {
                            if (genreCounts.ContainsKey(genre))
                            {
                                genreCounts[genre] += artistStat.ListenCount;
                            }
                            else
                            {
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

                var followStats = await _dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == request.UserId)
                    .Select(u => new
                    {
                        Followers = u.Friends.Count,
                        Following = u.FollowedArtists.Count + u.FollowedPlaylists.Count
                    })
                    .FirstAsync(cancellationToken);

                statistics.FollowersCount = followStats.Followers;
                statistics.FollowingCount = followStats.Following;

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
