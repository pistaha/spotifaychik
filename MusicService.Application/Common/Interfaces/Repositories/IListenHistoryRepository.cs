using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MusicService.Domain.Entities;

namespace MusicService.Application.Common.Interfaces.Repositories;

public interface IListenHistoryRepository : IBaseRepository<ListenHistory>
{
    Task<List<ListenHistory>> GetUserHistoryAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<List<Track>> GetRecentlyPlayedAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default);

    Task<List<Artist>> GetTopArtistsAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default);

    Task<List<Track>> GetTopTracksAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default);
}
