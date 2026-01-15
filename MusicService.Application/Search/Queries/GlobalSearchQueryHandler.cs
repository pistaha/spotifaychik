using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Search.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Search.Queries
{
    public class GlobalSearchQueryHandler : IRequestHandler<GlobalSearchQuery, GlobalSearchResultDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly ILogger<GlobalSearchQueryHandler> _logger;

        public GlobalSearchQueryHandler(
            IMusicServiceDbContext dbContext,
            ILogger<GlobalSearchQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<GlobalSearchResultDto> Handle(GlobalSearchQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Performing global search for query: {Query}", request.Query);

            var result = new GlobalSearchResultDto();
            var searchTerm = request.Query.ToLower();

            try
            {
                await ProcessGlobalArtistsAsync(result, searchTerm, request.Limit, cancellationToken);
                await ProcessGlobalAlbumsAsync(result, searchTerm, request.Limit, cancellationToken);
                await ProcessGlobalTracksAsync(result, searchTerm, request.Limit, cancellationToken);
                await ProcessGlobalPlaylistsAsync(result, searchTerm, request.Limit, cancellationToken);

                result.TotalResults = result.TopArtists.Count + result.TopAlbums.Count + 
                                     result.TopTracks.Count + result.TopPlaylists.Count;

                _logger.LogInformation("Global search completed: {TotalResults} results found", result.TotalResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing global search for query: {Query}", request.Query);
            }

            return result;
        }

        private async Task ProcessGlobalArtistsAsync(GlobalSearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            result.TopArtists = await _dbContext.Artists
                .AsNoTracking()
                .Where(a => EF.Functions.ILike(a.Name, $"%{searchTerm}%"))
                .OrderByDescending(a => a.MonthlyListeners)
                .Take(limit)
                .Select(a => new GlobalArtistDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    ImageUrl = a.ProfileImage
                })
                .ToListAsync(cancellationToken);
        }

        private async Task ProcessGlobalAlbumsAsync(GlobalSearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            result.TopAlbums = await _dbContext.Albums
                .AsNoTracking()
                .Where(a => EF.Functions.ILike(a.Title, $"%{searchTerm}%"))
                .OrderByDescending(a => a.ReleaseDate)
                .Take(limit)
                .Select(a => new GlobalAlbumDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    ImageUrl = a.CoverImage,
                    ArtistName = a.Artist != null ? a.Artist.Name : "Unknown"
                })
                .ToListAsync(cancellationToken);
        }

        private async Task ProcessGlobalTracksAsync(GlobalSearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            result.TopTracks = await _dbContext.Tracks
                .AsNoTracking()
                .Where(t => EF.Functions.ILike(t.Title, $"%{searchTerm}%"))
                .OrderByDescending(t => t.PlayCount)
                .Take(limit)
                .Select(t => new GlobalTrackDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    ArtistName = t.Artist != null ? t.Artist.Name : "Unknown",
                    Duration = t.DurationSeconds
                })
                .ToListAsync(cancellationToken);
        }

        private async Task ProcessGlobalPlaylistsAsync(GlobalSearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            result.TopPlaylists = await _dbContext.Playlists
                .AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Title, $"%{searchTerm}%"))
                .OrderByDescending(p => p.FollowersCount)
                .Take(limit)
                .Select(p => new GlobalPlaylistDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    ImageUrl = p.CoverImage,
                    CreatorName = p.CreatedBy != null ? p.CreatedBy.Username : "Unknown"
                })
                .ToListAsync(cancellationToken);
        }
    }
}
