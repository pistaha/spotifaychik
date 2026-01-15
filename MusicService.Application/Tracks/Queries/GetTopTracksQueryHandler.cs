using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Tracks.Queries
{
    public class GetTopTracksQueryHandler : IRequestHandler<GetTopTracksQuery, List<TrackDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;

        public GetTopTracksQueryHandler(
            IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<TrackDto>> Handle(GetTopTracksQuery request, CancellationToken cancellationToken)
        {
            List<TrackDto> topTracks;

            if (!string.IsNullOrEmpty(request.TimeRange) && request.TimeRange != "all")
            {
                var cutoffDate = GetCutoffDate(request.TimeRange);
                var trackData = await _dbContext.ListenHistories
                    .AsNoTracking()
                    .Where(h => h.ListenedAt >= cutoffDate)
                    .GroupBy(h => h.TrackId)
                    .Select(g => new { TrackId = g.Key, Plays = g.Count() })
                    .OrderByDescending(x => x.Plays)
                    .Take(request.Count)
                    .Join(_dbContext.Tracks.AsNoTracking()
                            .Include(t => t.Album)
                            .Include(t => t.Artist)
                            .AsSplitQuery(),
                        stats => stats.TrackId,
                        track => track.Id,
                        (stats, track) => new
                        {
                            track.Id,
                            track.CreatedAt,
                            track.UpdatedAt,
                            track.Title,
                            track.DurationSeconds,
                            track.TrackNumber,
                            track.PlayCount,
                            track.LikeCount,
                            track.IsExplicit,
                            track.AlbumId,
                            AlbumTitle = track.Album != null ? track.Album.Title : string.Empty,
                            AlbumCoverImage = track.Album != null ? track.Album.CoverImage : null,
                            track.ArtistId,
                            ArtistName = track.Artist != null ? track.Artist.Name : string.Empty
                        })
                    .ToListAsync(cancellationToken);

                topTracks = trackData.Select(t => new TrackDto
                {
                    Id = t.Id,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    Title = t.Title,
                    DurationSeconds = t.DurationSeconds,
                    DurationFormatted = FormatDuration(t.DurationSeconds),
                    TrackNumber = t.TrackNumber,
                    PlayCount = t.PlayCount,
                    LikeCount = t.LikeCount,
                    IsExplicit = t.IsExplicit,
                    IsLiked = false,
                    AlbumId = t.AlbumId,
                    AlbumTitle = t.AlbumTitle,
                    AlbumCoverImage = t.AlbumCoverImage,
                    ArtistId = t.ArtistId,
                    ArtistName = t.ArtistName
                }).ToList();
            }
            else
            {
                var tracks = await _dbContext.Tracks
                    .AsNoTracking()
                    .Include(t => t.Album)
                    .Include(t => t.Artist)
                    .AsSplitQuery()
                    .OrderByDescending(t => t.PlayCount)
                    .Take(request.Count)
                    .ToListAsync(cancellationToken);

                topTracks = tracks.Select(t => new TrackDto
                {
                    Id = t.Id,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    Title = t.Title,
                    DurationSeconds = t.DurationSeconds,
                    DurationFormatted = FormatDuration(t.DurationSeconds),
                    TrackNumber = t.TrackNumber,
                    PlayCount = t.PlayCount,
                    LikeCount = t.LikeCount,
                    IsExplicit = t.IsExplicit,
                    IsLiked = false,
                    AlbumId = t.AlbumId,
                    AlbumTitle = t.Album != null ? t.Album.Title : string.Empty,
                    AlbumCoverImage = t.Album?.CoverImage,
                    ArtistId = t.ArtistId,
                    ArtistName = t.Artist != null ? t.Artist.Name : string.Empty
                }).ToList();
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

        private static string FormatDuration(int durationSeconds)
        {
            var span = TimeSpan.FromSeconds(durationSeconds);
            return $"{(int)span.TotalMinutes}:{span.Seconds:00}";
        }
    }
}
