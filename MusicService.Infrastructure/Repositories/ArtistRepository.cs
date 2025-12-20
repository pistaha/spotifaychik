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
    public class ArtistRepository : FileStorageRepository<Artist>, IArtistRepository
    {
        public ArtistRepository(
            string filePath,
            ILogger<ArtistRepository> logger,
            IOptions<FileStorageOptions> options) : base(filePath, logger, options)
        {
        }

        public async Task<List<Artist>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var artists = await GetAllAsync(cancellationToken);
            return artists
                .Where(a => a.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           (a.RealName != null && a.RealName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public async Task<List<Artist>> GetTopArtistsAsync(int count, CancellationToken cancellationToken = default)
        {
            var artists = await GetAllAsync(cancellationToken);
            return artists
                .OrderByDescending(a => a.MonthlyListeners)
                .Take(count)
                .ToList();
        }

        public async Task<List<Artist>> GetArtistsByGenreAsync(string genre, CancellationToken cancellationToken = default)
        {
            var artists = await GetAllAsync(cancellationToken);
            return artists
                .Where(a => a.Genres.Any(g => g.Equals(genre, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public async Task<bool> FollowArtistAsync(Guid userId, Guid artistId, CancellationToken cancellationToken = default)
        {
            var artists = await GetAllAsync(cancellationToken);
            var artist = artists.FirstOrDefault(a => a.Id == artistId);
            
            if (artist == null)
                return false;

            // Здесь должна быть логика добавления пользователя в подписчики
            // В реальном проекте это было бы через отдельную таблицу Many-to-Many
            artist.MonthlyListeners++;
            await WriteAllAsync(artists, cancellationToken);
            return true;
        }

        public async Task<bool> UnfollowArtistAsync(Guid userId, Guid artistId, CancellationToken cancellationToken = default)
        {
            var artists = await GetAllAsync(cancellationToken);
            var artist = artists.FirstOrDefault(a => a.Id == artistId);
            
            if (artist == null)
                return false;

            artist.MonthlyListeners = Math.Max(0, artist.MonthlyListeners - 1);
            await WriteAllAsync(artists, cancellationToken);
            return true;
        }
    }
}
