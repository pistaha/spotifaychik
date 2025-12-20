using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MusicService.Domain.Entities;

namespace MusicService.Application.Common.Interfaces.Repositories;

public interface IArtistRepository : IBaseRepository<Artist>
{
    Task<List<Artist>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<Artist>> GetTopArtistsAsync(int count, CancellationToken cancellationToken = default);
    Task<List<Artist>> GetArtistsByGenreAsync(string genre, CancellationToken cancellationToken = default);
    Task<bool> FollowArtistAsync(Guid userId, Guid artistId, CancellationToken cancellationToken = default);
    Task<bool> UnfollowArtistAsync(Guid userId, Guid artistId, CancellationToken cancellationToken = default);
}
