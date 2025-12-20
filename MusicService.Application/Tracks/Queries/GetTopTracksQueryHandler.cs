using AutoMapper;
using MediatR;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Tracks.Queries
{
    public class GetTopTracksQueryHandler : IRequestHandler<GetTopTracksQuery, List<TrackDto>>
    {
        private readonly ITrackRepository _trackRepository;
        private readonly IListenHistoryRepository _listenHistoryRepository;
        private readonly IMapper _mapper;

        public GetTopTracksQueryHandler(
            ITrackRepository trackRepository,
            IListenHistoryRepository listenHistoryRepository,
            IMapper mapper)
        {
            _trackRepository = trackRepository;
            _listenHistoryRepository = listenHistoryRepository;
            _mapper = mapper;
        }

        public async Task<List<TrackDto>> Handle(GetTopTracksQuery request, CancellationToken cancellationToken)
        {
            List<TrackDto> topTracks;
            
            // Если указан временной диапазон, используем историю прослушиваний
            if (!string.IsNullOrEmpty(request.TimeRange) && request.TimeRange != "all")
            {
                var cutoffDate = GetCutoffDate(request.TimeRange);
                var allHistory = await _listenHistoryRepository.GetAllAsync(cancellationToken);
                
                var recentHistory = allHistory
                    .Where(h => h.ListenedAt >= cutoffDate)
                    .ToList();

                var trackPlays = recentHistory
                    .GroupBy(h => h.TrackId)
                    .Select(g => new
                    {
                        TrackId = g.Key,
                        PlayCount = g.Count()
                    })
                    .OrderByDescending(x => x.PlayCount)
                    .Take(request.Count)
                    .ToList();

                // Получаем треки по ID
                var tracks = new List<Domain.Entities.Track>();
                foreach (var trackPlay in trackPlays)
                {
                    var track = await _trackRepository.GetByIdAsync(trackPlay.TrackId, cancellationToken);
                    if (track != null)
                    {
                        tracks.Add(track);
                    }
                }

                topTracks = _mapper.Map<List<TrackDto>>(tracks);
            }
            else
            {
                // Используем общий счетчик прослушиваний
                var tracks = await _trackRepository.GetTopTracksAsync(request.Count, cancellationToken);
                topTracks = _mapper.Map<List<TrackDto>>(tracks);
            }

            return topTracks;
        }

        private DateTime GetCutoffDate(string timeRange)
        {
            return timeRange.ToLower() switch
            {
                "day" => DateTime.UtcNow.AddDays(-1),
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddDays(-30),
                "year" => DateTime.UtcNow.AddDays(-365),
                _ => DateTime.MinValue
            };
        }
    }
}