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
    public class PlaylistRepository : FileStorageRepository<Playlist>, IPlaylistRepository
    {
        public PlaylistRepository(
            string filePath,
            ILogger<PlaylistRepository> logger,
            IOptions<FileStorageOptions> options) : base(filePath, logger, options)
        {
        }

        public async Task<List<Playlist>> GetUserPlaylistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var playlists = await GetAllAsync(cancellationToken);
            return playlists
                .Where(p => p.CreatedById == userId || (p.IsPublic && p.Followers.Any(f => f.Id == userId)))
                .ToList();
        }

        public async Task<List<Playlist>> GetPublicPlaylistsAsync(CancellationToken cancellationToken = default)
        {
            var playlists = await GetAllAsync(cancellationToken);
            return playlists.Where(p => p.IsPublic).ToList();
        }

        public async Task<List<Playlist>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var playlists = await GetAllAsync(cancellationToken);
            return playlists
                .Where(p => p.IsPublic && 
                    (p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                     (p.Description != null && p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        public async Task<bool> AddTrackToPlaylistAsync(Guid playlistId, Guid trackId, Guid userId, CancellationToken cancellationToken = default)
        {
            var playlists = await GetAllAsync(cancellationToken);
            var playlist = playlists.FirstOrDefault(p => p.Id == playlistId);
            
            if (playlist == null)
                return false;

            // Проверяем права на редактирование
            if (playlist.CreatedById != userId && !playlist.IsCollaborative)
                return false;

            // Здесь должна быть логика добавления трека в плейлист
            // В реальном проекте это было бы через отдельную сущность PlaylistTrack
            return true;
        }

        public async Task<bool> RemoveTrackFromPlaylistAsync(Guid playlistId, Guid trackId, CancellationToken cancellationToken = default)
        {
            var playlists = await GetAllAsync(cancellationToken);
            var playlist = playlists.FirstOrDefault(p => p.Id == playlistId);
            
            if (playlist == null)
                return false;

            // Здесь должна быть логика удаления трека из плейлиста
            return true;
        }

        public async Task<bool> FollowPlaylistAsync(Guid playlistId, Guid userId, CancellationToken cancellationToken = default)
        {
            var playlists = await GetAllAsync(cancellationToken);
            var playlist = playlists.FirstOrDefault(p => p.Id == playlistId);
            
            if (playlist == null || !playlist.IsPublic)
                return false;

            playlist.FollowersCount++;
            await WriteAllAsync(playlists, cancellationToken);
            return true;
        }
    }
}
