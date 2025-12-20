using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Configuration;

namespace MusicService.Infrastructure.Repositories
{
    public class ListenHistoryRepository : FileStorageRepository<ListenHistory>, IListenHistoryRepository
    {
        public ListenHistoryRepository(
            string filePath,
            ILogger<ListenHistoryRepository> logger,
            IOptions<FileStorageOptions> options) : base(filePath, logger, options)
        {
        }

        public async Task<List<ListenHistory>> GetUserHistoryAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var history = await GetAllAsync(cancellationToken);
            var query = history.Where(h => h.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(h => h.ListenedAt >= fromDate.Value);
            
            if (toDate.HasValue)
                query = query.Where(h => h.ListenedAt <= toDate.Value);

            return query.OrderByDescending(h => h.ListenedAt).ToList();
        }

        public async Task<List<Track>> GetRecentlyPlayedAsync(Guid userId, int count, CancellationToken cancellationToken = default)
        {
            var history = await GetAllAsync(cancellationToken);
            var recentTracks = history
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ListenedAt)
                .Take(count)
                .Select(h => h.Track)
                .Where(t => t != null)
                .ToList();

            return recentTracks!;
        }

        public async Task<List<Artist>> GetTopArtistsAsync(Guid userId, int count, CancellationToken cancellationToken = default)
        {
            var history = await GetAllAsync(cancellationToken);
            var userHistory = history.Where(h => h.UserId == userId && h.Track != null && h.Track.Artist != null);

            var topArtists = userHistory
                .GroupBy(h => h.Track!.ArtistId)
                .Select(g => new
                {
                    Artist = g.First().Track!.Artist,
                    PlayCount = g.Count()
                })
                .OrderByDescending(x => x.PlayCount)
                .Take(count)
                .Select(x => x.Artist!)
                .ToList();

            return topArtists;
        }

        public async Task<List<Track>> GetTopTracksAsync(Guid userId, int count, CancellationToken cancellationToken = default)
        {
            var history = await GetAllAsync(cancellationToken);
            var userHistory = history.Where(h => h.UserId == userId && h.Track != null);

            var topTracks = userHistory
                .GroupBy(h => h.TrackId)
                .Select(g => new
                {
                    Track = g.First().Track,
                    PlayCount = g.Count()
                })
                .OrderByDescending(x => x.PlayCount)
                .Take(count)
                .Select(x => x.Track!)
                .ToList();

            return topTracks;
        }
    }
}
