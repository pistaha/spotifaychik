using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces.Repositories;
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
        private readonly IArtistRepository _artistRepository;
        private readonly IAlbumRepository _albumRepository;
        private readonly ITrackRepository _trackRepository;
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GlobalSearchQueryHandler> _logger;

        public GlobalSearchQueryHandler(
            IArtistRepository artistRepository,
            IAlbumRepository albumRepository,
            ITrackRepository trackRepository,
            IPlaylistRepository playlistRepository,
            IUserRepository userRepository,
            ILogger<GlobalSearchQueryHandler> logger)
        {
            _artistRepository = artistRepository;
            _albumRepository = albumRepository;
            _trackRepository = trackRepository;
            _playlistRepository = playlistRepository;
            _userRepository = userRepository;
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
            var artists = await _artistRepository.SearchAsync(searchTerm, cancellationToken);
            result.TopArtists = artists
                .OrderByDescending(a => a.MonthlyListeners)
                .Take(limit)
                .Select(a => new GlobalArtistDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    ImageUrl = a.ProfileImage
                })
                .ToList();
        }

        private async Task ProcessGlobalAlbumsAsync(GlobalSearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var albums = await _albumRepository.SearchAsync(searchTerm, cancellationToken);
            var allArtists = await _artistRepository.GetAllAsync(cancellationToken);
            
            result.TopAlbums = albums
                .OrderByDescending(a => a.ReleaseDate)
                .Take(limit)
                .Select(a => new GlobalAlbumDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    ImageUrl = a.CoverImage,
                    ArtistName = allArtists.FirstOrDefault(artist => artist.Id == a.ArtistId)?.Name ?? "Unknown"
                })
                .ToList();
        }

        private async Task ProcessGlobalTracksAsync(GlobalSearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var tracks = await _trackRepository.SearchAsync(searchTerm, cancellationToken);
            var allArtists = await _artistRepository.GetAllAsync(cancellationToken);
            
            result.TopTracks = tracks
                .OrderByDescending(t => t.PlayCount)
                .Take(limit)
                .Select(t => new GlobalTrackDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    ArtistName = allArtists.FirstOrDefault(artist => artist.Id == t.ArtistId)?.Name ?? "Unknown",
                    Duration = t.DurationSeconds
                })
                .ToList();
        }

        private async Task ProcessGlobalPlaylistsAsync(GlobalSearchResultDto result, string searchTerm, int limit, CancellationToken cancellationToken)
        {
            var playlists = await _playlistRepository.SearchAsync(searchTerm, cancellationToken);
            var allUsers = await _userRepository.GetAllAsync(cancellationToken);
            
            result.TopPlaylists = playlists
                .OrderByDescending(p => p.FollowersCount)
                .Take(limit)
                .Select(p => new GlobalPlaylistDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    ImageUrl = p.CoverImage,
                    CreatorName = allUsers.FirstOrDefault(user => user.Id == p.CreatedById)?.Username ?? "Unknown"
                })
                .ToList();
        }
    }
}
