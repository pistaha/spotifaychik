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
    public class AlbumRepository : FileStorageRepository<Album>, IAlbumRepository
    {
        public AlbumRepository(
            string filePath,
            ILogger<AlbumRepository> logger,
            IOptions<FileStorageOptions> options) : base(filePath, logger, options)
        {
        }

        public async Task<List<Album>> GetAlbumsByArtistAsync(Guid artistId, CancellationToken cancellationToken = default)
        {
            var albums = await GetAllAsync(cancellationToken);
            return albums.Where(a => a.ArtistId == artistId).ToList();
        }

        public async Task<List<Album>> GetRecentReleasesAsync(int days = 30, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var albums = await GetAllAsync(cancellationToken);
            return albums.Where(a => a.ReleaseDate >= cutoffDate).ToList();
        }

        public async Task<List<Album>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var albums = await GetAllAsync(cancellationToken);
            return albums
                .Where(a => a.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            (a.Description != null && a.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public async Task<List<Album>> GetAlbumsByGenreAsync(string genre, CancellationToken cancellationToken = default)
        {
            var albums = await GetAllAsync(cancellationToken);
            return albums
                .Where(a => a.Genres.Any(g => g.Equals(genre, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public async Task<PagedResult<Album>> GetPagedAlbumsAsync(
            int page, 
            int pageSize, 
            string? search = null,
            string? genre = null,
            string? sortBy = "CreatedAt",
            string? sortOrder = "desc",
            CancellationToken cancellationToken = default)
        {
            var albums = await GetAllAsync(cancellationToken);
            
            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                albums = albums.Where(a => 
                    a.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (a.Description != null && a.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // Apply genre filter
            if (!string.IsNullOrEmpty(genre))
            {
                albums = albums.Where(a => 
                    a.Genres.Any(g => g.Equals(genre, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // Apply sorting
            albums = sortBy?.ToLower() switch
            {
                "title" => sortOrder == "asc" ? 
                    albums.OrderBy(a => a.Title).ToList() : 
                    albums.OrderByDescending(a => a.Title).ToList(),
                "releasedate" => sortOrder == "asc" ?
                    albums.OrderBy(a => a.ReleaseDate).ToList() :
                    albums.OrderByDescending(a => a.ReleaseDate).ToList(),
                _ => sortOrder == "asc" ?
                    albums.OrderBy(a => a.CreatedAt).ToList() :
                    albums.OrderByDescending(a => a.CreatedAt).ToList()
            };

            // Apply pagination
            var totalCount = albums.Count;
            var items = albums
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<Album>(items, totalCount, page, pageSize);
        }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        public PagedResult(List<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }
    }
}
