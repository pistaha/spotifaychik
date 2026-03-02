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
    public class SearchQueryHandler : IRequestHandler<SearchQuery, SearchResultDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly ILogger<SearchQueryHandler> _logger;

        public SearchQueryHandler(
            IMusicServiceDbContext dbContext,
            ILogger<SearchQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        public async Task<SearchResultDto> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Performing search for query: {Query}, type: {Type}", 
                request.Query, request.Type ?? "all");

            var result = new SearchResultDto();
            var searchTerm = request.Query.Trim().ToLower();

            try
            {
                var type = request.Type?.ToLowerInvariant();
                var searchAll = string.IsNullOrEmpty(type) || type == "all";

                if (searchAll || type == "artist")
                {
                    await SearchArtistsAsync(result, searchTerm, request.Limit, cancellationToken);
                }

                if (searchAll || type == "album")
                {
                    await SearchAlbumsAsync(result, searchTerm, request.Limit, cancellationToken);
                }

                if (searchAll || type == "track")
                {
                    await SearchTracksAsync(result, searchTerm, request.Limit, cancellationToken);
                }

                if (searchAll || type == "playlist")
                {
                    await SearchPlaylistsAsync(result, searchTerm, request.Limit, cancellationToken);
                }

                if (searchAll || type == "user")
                {
                    await SearchUsersAsync(result, searchTerm, request.Limit, cancellationToken);
                }

                result.TotalResults = result.Artists.Count + result.Albums.Count + 
                                     result.Tracks.Count + result.Playlists.Count + 
                                     result.Users.Count;

                _logger.LogInformation("Search completed: {TotalResults} results found", result.TotalResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search for query: {Query}", request.Query);
            }

            return result;
        }

        private async Task SearchArtistsAsync(SearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var artists = await _dbContext.Artists
                .AsNoTracking()
                .Where(a => EF.Functions.ILike(a.Name, $"%{searchTerm}%") ||
                            a.Genres.Any(g => EF.Functions.ILike(g, $"%{searchTerm}%")))
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.ProfileImage,
                    a.Genres,
                    a.MonthlyListeners
                })
                .ToListAsync(cancellationToken);

            result.Artists = artists
                .Select(a => new ArtistSearchResultDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    ProfileImage = a.ProfileImage,
                    Genres = a.Genres,
                    MonthlyListeners = a.MonthlyListeners,
                    Relevance = CalculateRelevance(a.Name, searchTerm, a.Genres)
                })
                .OrderByDescending(a => a.Relevance)
                .Take(limit)
                .ToList();
        }

        private async Task SearchAlbumsAsync(SearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var albums = await _dbContext.Albums
                .AsNoTracking()
                .Where(a => EF.Functions.ILike(a.Title, $"%{searchTerm}%") ||
                            a.Genres.Any(g => EF.Functions.ILike(g, $"%{searchTerm}%")))
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.CoverImage,
                    ArtistName = a.Artist != null ? a.Artist.Name : "Unknown",
                    ReleaseYear = a.ReleaseDate.Year,
                    a.Genres
                })
                .ToListAsync(cancellationToken);

            result.Albums = albums
                .Select(a => new AlbumSearchResultDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    CoverImage = a.CoverImage,
                    ArtistName = a.ArtistName,
                    ReleaseYear = a.ReleaseYear,
                    Relevance = CalculateRelevance(a.Title, searchTerm, a.Genres)
                })
                .OrderByDescending(a => a.Relevance)
                .Take(limit)
                .ToList();
        }

        private async Task SearchTracksAsync(SearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var tracks = await _dbContext.Tracks
                .AsNoTracking()
                .Where(t => EF.Functions.ILike(t.Title, $"%{searchTerm}%"))
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    ArtistName = t.Artist != null ? t.Artist.Name : "Unknown",
                    AlbumTitle = t.Album != null ? t.Album.Title : "Unknown",
                    t.DurationSeconds
                })
                .ToListAsync(cancellationToken);

            result.Tracks = tracks
                .Select(t => new TrackSearchResultDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    ArtistName = t.ArtistName,
                    AlbumTitle = t.AlbumTitle,
                    DurationSeconds = t.DurationSeconds,
                    Relevance = CalculateRelevance(t.Title, searchTerm)
                })
                .OrderByDescending(t => t.Relevance)
                .Take(limit)
                .ToList();
        }

        private async Task SearchPlaylistsAsync(SearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var playlists = await _dbContext.Playlists
                .AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Title, $"%{searchTerm}%"))
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.CoverImage,
                    CreatorName = p.CreatedBy != null ? p.CreatedBy.Username : "Unknown",
                    TrackCount = p.PlaylistTracks.Count,
                    p.FollowersCount
                })
                .ToListAsync(cancellationToken);

            result.Playlists = playlists
                .Select(p => new PlaylistSearchResultDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CoverImage = p.CoverImage,
                    CreatorName = p.CreatorName,
                    TrackCount = p.TrackCount,
                    FollowersCount = p.FollowersCount,
                    Relevance = CalculateRelevance(p.Title, searchTerm)
                })
                .OrderByDescending(p => p.Relevance)
                .Take(limit)
                .ToList();
        }

        private async Task SearchUsersAsync(SearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var users = await _dbContext.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted &&
                            (EF.Functions.ILike(u.Username, $"%{searchTerm}%") ||
                             (u.DisplayName != null && EF.Functions.ILike(u.DisplayName, $"%{searchTerm}%"))))
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.DisplayName,
                    u.ProfileImage
                })
                .ToListAsync(cancellationToken);

            result.Users = users
                .Select(u => new UserSearchResultDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    ProfileImage = u.ProfileImage,
                    Relevance = CalculateRelevance(u.Username + " " + u.DisplayName, searchTerm)
                })
                .OrderByDescending(u => u.Relevance)
                .Take(limit)
                .ToList();
        }

        private double CalculateRelevance(string text, string searchTerm, List<string>? genres = null)
        {
            var textLower = text.ToLower();
            var searchTermLower = searchTerm.ToLower();

            if (textLower.Contains(searchTermLower))
            {
                if (textLower.StartsWith(searchTermLower))
                    return 1.0;
                return 0.5;
            }

            if (genres != null)
            {
                foreach (var genre in genres)
                {
                    if (genre.ToLower().Contains(searchTermLower))
                        return 0.3;
                }
            }

            var searchWords = searchTermLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var textWords = textLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var searchWord in searchWords)
            {
                foreach (var textWord in textWords)
                {
                    if (textWord.Contains(searchWord) || searchWord.Contains(textWord))
                        return 0.2;
                }
            }

            return 0.0;
        }
    }
}
