using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MusicService.Domain.Entities;

namespace MusicService.Application.Common.Interfaces.Repositories;

public interface IAlbumRepository : IBaseRepository<Album>
{
    Task<List<Album>> GetAlbumsByArtistAsync(Guid artistId, CancellationToken cancellationToken = default);
    Task<List<Album>> GetRecentReleasesAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<List<Album>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}