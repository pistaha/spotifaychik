using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Search.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Search.Queries
{
    public class AdvancedSearchQueryHandler : IRequestHandler<AdvancedSearchQuery, PagedResult<AdvancedSearchResultDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly ILogger<AdvancedSearchQueryHandler> _logger;

        public AdvancedSearchQueryHandler(
            IMusicServiceDbContext dbContext,
            ILogger<AdvancedSearchQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<PagedResult<AdvancedSearchResultDto>> Handle(AdvancedSearchQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Performing advanced search with parameters");

            var allResults = new List<AdvancedSearchResultDto>();
            var searchTerm = request.Request.Search?.ToLower() ?? string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    await ProcessArtistsAsync(allResults, searchTerm, cancellationToken);
                    await ProcessAlbumsAsync(allResults, searchTerm, cancellationToken);
                    await ProcessTracksAsync(allResults, searchTerm, cancellationToken);
                    await ProcessPlaylistsAsync(allResults, searchTerm, cancellationToken);
                    await ProcessUsersAsync(allResults, searchTerm, cancellationToken);
                }

                ApplyFilters(ref allResults, request.Request);
                ApplySorting(ref allResults, request.Request);

                var pageNumber = request.Request.PageNumber;
                var pageSize = request.Request.PageSize;
                var totalCount = allResults.Count;

                var items = allResults
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Advanced search completed: {TotalCount} results found", totalCount);

                return new PagedResult<AdvancedSearchResultDto>(items, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing advanced search");
                return new PagedResult<AdvancedSearchResultDto>(new List<AdvancedSearchResultDto>(), 0, 1, 10);
            }
        }

        private async Task ProcessArtistsAsync(List<AdvancedSearchResultDto> results, string searchTerm, CancellationToken cancellationToken)
        {
            var artists = await _dbContext.Artists
                .AsNoTracking()
                .Where(a => EF.Functions.ILike(a.Name, $"%{searchTerm}%") ||
                            a.Genres.Any(g => EF.Functions.ILike(g, $"%{searchTerm}%")))
                .ToListAsync(cancellationToken);

            foreach (var artist in artists)
            {
                results.Add(new AdvancedSearchResultDto
                {
                    Id = artist.Id,
                    Type = "artist",
                    Title = artist.Name,
                    Subtitle = string.Join(", ", artist.Genres.Take(3)),
                    ImageUrl = artist.ProfileImage,
                    Metadata = new Dictionary<string, object>
                    {
                        { "genres", artist.Genres },
                        { "monthlyListeners", artist.MonthlyListeners },
                        { "isVerified", artist.IsVerified }
                    },
                    RelevanceScore = CalculateAdvancedRelevance(artist.Name, searchTerm, artist.Genres),
                    CreatedAt = artist.CreatedAt
                });
            }
        }

        private async Task ProcessAlbumsAsync(List<AdvancedSearchResultDto> results, string searchTerm, CancellationToken cancellationToken)
        {
            var albums = await _dbContext.Albums
                .AsNoTracking()
                .Where(a => EF.Functions.ILike(a.Title, $"%{searchTerm}%") ||
                            a.Genres.Any(g => EF.Functions.ILike(g, $"%{searchTerm}%")))
                .Select(a => new
                {
                    Album = a,
                    ArtistName = a.Artist != null ? a.Artist.Name : "Unknown",
                    TrackCount = a.Tracks.Count
                })
                .ToListAsync(cancellationToken);

            foreach (var item in albums)
            {
                var album = item.Album;
                results.Add(new AdvancedSearchResultDto
                {
                    Id = album.Id,
                    Type = "album",
                    Title = album.Title,
                    Subtitle = item.ArtistName,
                    ImageUrl = album.CoverImage,
                    Metadata = new Dictionary<string, object>
                    {
                        { "releaseDate", album.ReleaseDate },
                        { "genres", album.Genres },
                        { "trackCount", item.TrackCount }
                    },
                    RelevanceScore = CalculateAdvancedRelevance(album.Title, searchTerm, album.Genres),
                    CreatedAt = album.CreatedAt
                });
            }
        }

        private async Task ProcessTracksAsync(List<AdvancedSearchResultDto> results, string searchTerm, CancellationToken cancellationToken)
        {
            var tracks = await _dbContext.Tracks
                .AsNoTracking()
                .Where(t => EF.Functions.ILike(t.Title, $"%{searchTerm}%"))
                .Select(t => new
                {
                    Track = t,
                    ArtistName = t.Artist != null ? t.Artist.Name : "Unknown",
                    AlbumTitle = t.Album != null ? t.Album.Title : "Unknown"
                })
                .ToListAsync(cancellationToken);

            foreach (var item in tracks)
            {
                var track = item.Track;
                results.Add(new AdvancedSearchResultDto
                {
                    Id = track.Id,
                    Type = "track",
                    Title = track.Title,
                    Subtitle = $"{item.ArtistName} • {item.AlbumTitle}",
                    Metadata = new Dictionary<string, object>
                    {
                        { "duration", track.DurationSeconds },
                        { "playCount", track.PlayCount },
                        { "artistId", track.ArtistId }
                    },
                    RelevanceScore = CalculateAdvancedRelevance(track.Title, searchTerm),
                    CreatedAt = track.CreatedAt
                });
            }
        }

        private async Task ProcessPlaylistsAsync(List<AdvancedSearchResultDto> results, string searchTerm, CancellationToken cancellationToken)
        {
            var playlists = await _dbContext.Playlists
                .AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Title, $"%{searchTerm}%"))
                .Select(p => new
                {
                    Playlist = p,
                    CreatorName = p.CreatedBy != null ? p.CreatedBy.Username : "Unknown",
                    TrackCount = p.PlaylistTracks.Count
                })
                .ToListAsync(cancellationToken);

            foreach (var item in playlists)
            {
                var playlist = item.Playlist;
                results.Add(new AdvancedSearchResultDto
                {
                    Id = playlist.Id,
                    Type = "playlist",
                    Title = playlist.Title,
                    Subtitle = item.CreatorName,
                    ImageUrl = playlist.CoverImage,
                    Metadata = new Dictionary<string, object>
                    {
                        { "trackCount", item.TrackCount },
                        { "followersCount", playlist.FollowersCount },
                        { "isPublic", playlist.IsPublic }
                    },
                    RelevanceScore = CalculateAdvancedRelevance(playlist.Title, searchTerm),
                    CreatedAt = playlist.CreatedAt
                });
            }
        }

        private async Task ProcessUsersAsync(List<AdvancedSearchResultDto> results, string searchTerm, CancellationToken cancellationToken)
        {
            var users = await _dbContext.Users
                .AsNoTracking()
                .Where(u => EF.Functions.ILike(u.Username, $"%{searchTerm}%") ||
                            (u.DisplayName != null && EF.Functions.ILike(u.DisplayName, $"%{searchTerm}%")))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                results.Add(new AdvancedSearchResultDto
                {
                    Id = user.Id,
                    Type = "user",
                    Title = user.DisplayName,
                    Subtitle = user.Username,
                    ImageUrl = user.ProfileImage,
                    Metadata = new Dictionary<string, object>
                    {
                        { "country", user.Country },
                        { "favoriteGenres", user.FavoriteGenres }
                    },
                    RelevanceScore = CalculateAdvancedRelevance(user.Username + " " + user.DisplayName, searchTerm),
                    CreatedAt = user.CreatedAt
                });
            }
        }

        private void ApplyFilters(ref List<AdvancedSearchResultDto> results, AdvancedPaginationRequest request)
        {
            if (request.CreatedFrom.HasValue)
            {
                results = results.Where(r => r.CreatedAt >= request.CreatedFrom.Value).ToList();
            }

            if (request.CreatedTo.HasValue)
            {
                results = results.Where(r => r.CreatedAt <= request.CreatedTo.Value).ToList();
            }

            if (request.Categories != null && request.Categories.Length > 0)
            {
                results = results.Where(r => 
                    request.Categories.Contains(r.Type, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        private void ApplySorting(ref List<AdvancedSearchResultDto> results, AdvancedPaginationRequest request)
        {
            results = request.SortBy?.ToLower() switch
            {
                "title" => request.SortOrder == "asc" ? 
                    results.OrderBy(r => r.Title).ToList() : 
                    results.OrderByDescending(r => r.Title).ToList(),
                "relevance" => results.OrderByDescending(r => r.RelevanceScore).ToList(),
                _ => request.SortOrder == "asc" ?
                    results.OrderBy(r => r.CreatedAt).ToList() :
                    results.OrderByDescending(r => r.CreatedAt).ToList()
            };
        }

        private double CalculateAdvancedRelevance(string text, string searchTerm, List<string>? genres = null)
        {
            var textLower = text.ToLower();
            var searchTermLower = searchTerm.ToLower();

            double score = 0.0;

            if (textLower == searchTermLower)
                score += 2.0;
            else if (textLower.StartsWith(searchTermLower))
                score += 1.5;
            else if (textLower.Contains(searchTermLower))
                score += 1.0;

            var searchWords = searchTermLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var textWords = textLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var matchingWords = searchWords.Count(sw => 
                textWords.Any(tw => tw.Contains(sw) || sw.Contains(tw)));

            if (matchingWords > 0)
                score += matchingWords * 0.3;

            if (genres != null)
            {
                var genreMatches = genres.Count(g => 
                    g.ToLower().Contains(searchTermLower) || 
                    searchWords.Any(sw => g.ToLower().Contains(sw)));

                if (genreMatches > 0)
                    score += genreMatches * 0.2;
            }

            return Math.Min(score, 3.0);
        }
    }
}
