using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Search.Dtos;
using MusicService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Search.Queries
{
    public class SearchQueryHandler : IRequestHandler<SearchQuery, SearchResultDto>
    {
        private readonly IArtistRepository _artistRepository;
        private readonly IAlbumRepository _albumRepository;
        private readonly ITrackRepository _trackRepository;
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SearchQueryHandler> _logger;

        public SearchQueryHandler(
            IArtistRepository artistRepository,
            IAlbumRepository albumRepository,
            ITrackRepository trackRepository,
            IPlaylistRepository playlistRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<SearchQueryHandler> logger)
        {
            _artistRepository = artistRepository;
            _albumRepository = albumRepository;
            _trackRepository = trackRepository;
            _playlistRepository = playlistRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<SearchResultDto> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Performing search for query: {Query}, type: {Type}", 
                request.Query, request.Type ?? "all");

            var result = new SearchResultDto();
            var searchTerm = request.Query.ToLower();

            try
            {
                var searchAll = string.IsNullOrEmpty(request.Type) || request.Type.ToLower() == "all";

                if (searchAll || request.Type.ToLower() == "artist")
                {
                    await SearchArtistsAsync(result, searchTerm, request.Limit, cancellationToken);
                }

                if (searchAll || request.Type.ToLower() == "album")
                {
                    await SearchAlbumsAsync(result, searchTerm, request.Limit, cancellationToken);
                }

                if (searchAll || request.Type.ToLower() == "track")
                {
                    await SearchTracksAsync(result, searchTerm, request.Limit, cancellationToken);
                }

                if (searchAll || request.Type.ToLower() == "playlist")
                {
                    await SearchPlaylistsAsync(result, searchTerm, request.Limit, cancellationToken);
                }

                if (searchAll || request.Type.ToLower() == "user")
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
            var artists = await _artistRepository.SearchAsync(searchTerm, cancellationToken);
            var allArtists = await _artistRepository.GetAllAsync(cancellationToken);
            
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
            var albums = await _albumRepository.SearchAsync(searchTerm, cancellationToken);
            var allArtists = await _artistRepository.GetAllAsync(cancellationToken);
            
            result.Albums = albums
                .Select(a => new AlbumSearchResultDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    CoverImage = a.CoverImage,
                    ArtistName = allArtists.FirstOrDefault(artist => artist.Id == a.ArtistId)?.Name ?? "Unknown",
                    ReleaseYear = a.ReleaseDate.Year,
                    Relevance = CalculateRelevance(a.Title, searchTerm, a.Genres)
                })
                .OrderByDescending(a => a.Relevance)
                .Take(limit)
                .ToList();
        }

        private async Task SearchTracksAsync(SearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var tracks = await _trackRepository.SearchAsync(searchTerm, cancellationToken);
            var allArtists = await _artistRepository.GetAllAsync(cancellationToken);
            var allAlbums = await _albumRepository.GetAllAsync(cancellationToken);
            
            result.Tracks = tracks
                .Select(t => new TrackSearchResultDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    ArtistName = allArtists.FirstOrDefault(artist => artist.Id == t.ArtistId)?.Name ?? "Unknown",
                    AlbumTitle = allAlbums.FirstOrDefault(album => album.Id == t.AlbumId)?.Title ?? "Unknown",
                    DurationSeconds = t.DurationSeconds,
                    Relevance = CalculateRelevance(t.Title, searchTerm)
                })
                .OrderByDescending(t => t.Relevance)
                .Take(limit)
                .ToList();
        }

        private async Task SearchPlaylistsAsync(SearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var playlists = await _playlistRepository.SearchAsync(searchTerm, cancellationToken);
            var allUsers = await _userRepository.GetAllAsync(cancellationToken);
            
            result.Playlists = playlists
                .Select(p => new PlaylistSearchResultDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CoverImage = p.CoverImage,
                    CreatorName = allUsers.FirstOrDefault(user => user.Id == p.CreatedById)?.Username ?? "Unknown",
                    TrackCount = p.PlaylistTracks?.Count ?? 0,
                    FollowersCount = p.FollowersCount,
                    Relevance = CalculateRelevance(p.Title, searchTerm)
                })
                .OrderByDescending(p => p.Relevance)
                .Take(limit)
                .ToList();
        }

        private async Task SearchUsersAsync(SearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var users = await _userRepository.SearchUsersAsync(searchTerm, cancellationToken);
            
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