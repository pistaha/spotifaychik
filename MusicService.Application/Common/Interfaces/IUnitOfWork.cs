using System.Threading;
using System.Threading.Tasks;
using MusicService.Application.Common.Interfaces.Repositories;

namespace MusicService.Application.Common.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IArtistRepository Artists { get; }
        IAlbumRepository Albums { get; }
        ITrackRepository Tracks { get; }
        IPlaylistRepository Playlists { get; }
        IListenHistoryRepository ListenHistories { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
