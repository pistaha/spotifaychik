using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MusicService.Domain.Entities;

namespace MusicService.Application.Common.Interfaces.Repositories;

public interface IPlaylistRepository : IBaseRepository<Playlist>
{
    Task<List<Playlist>> GetUserPlaylistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Playlist>> GetPublicPlaylistsAsync(CancellationToken cancellationToken = default);
    Task<List<Playlist>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<bool> AddTrackToPlaylistAsync(Guid playlistId, Guid trackId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RemoveTrackFromPlaylistAsync(Guid playlistId, Guid trackId, CancellationToken cancellationToken = default);
    Task<bool> FollowPlaylistAsync(Guid playlistId, Guid userId, CancellationToken cancellationToken = default);
}
