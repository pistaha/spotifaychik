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
    public class TrackRepository : FileStorageRepository<Track>, ITrackRepository
    {
        public TrackRepository(
            string filePath,
            ILogger<TrackRepository> logger,
            IOptions<FileStorageOptions> options) : base(filePath, logger, options)
        {
        }

        public async Task<List<Track>> GetTracksByAlbumAsync(Guid albumId, CancellationToken cancellationToken = default)
        {
            var tracks = await GetAllAsync(cancellationToken);
            return tracks.Where(t => t.AlbumId == albumId).OrderBy(t => t.TrackNumber).ToList();
        }

        public async Task<List<Track>> GetTracksByArtistAsync(Guid artistId, CancellationToken cancellationToken = default)
        {
            var tracks = await GetAllAsync(cancellationToken);
            return tracks.Where(t => t.ArtistId == artistId).ToList();
        }

        public async Task<List<Track>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var tracks = await GetAllAsync(cancellationToken);
            return tracks
                .Where(t => t.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           (t.Lyrics != null && t.Lyrics.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public async Task<List<Track>> GetTopTracksAsync(int count, CancellationToken cancellationToken = default)
        {
            var tracks = await GetAllAsync(cancellationToken);
            return tracks
                .OrderByDescending(t => t.PlayCount)
                .Take(count)
                .ToList();
        }

        public async Task IncrementPlayCountAsync(Guid trackId, CancellationToken cancellationToken = default)
        {
            var tracks = await GetAllAsync(cancellationToken);
            var track = tracks.FirstOrDefault(t => t.Id == trackId);
            
            if (track != null)
            {
                track.IncrementPlayCount();
                await WriteAllAsync(tracks, cancellationToken);
            }
        }
    }
}
