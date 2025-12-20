using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MusicService.Domain.Entities;

namespace MusicService.Application.Common.Interfaces.Repositories;

public interface ITrackRepository : IBaseRepository<Track>
{
    Task<List<Track>> GetTracksByAlbumAsync(Guid albumId, CancellationToken cancellationToken = default);
    Task<List<Track>> GetTracksByArtistAsync(Guid artistId, CancellationToken cancellationToken = default);
    Task<List<Track>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<Track>> GetTopTracksAsync(int count, CancellationToken cancellationToken = default);
    Task IncrementPlayCountAsync(Guid trackId, CancellationToken cancellationToken = default);
}
