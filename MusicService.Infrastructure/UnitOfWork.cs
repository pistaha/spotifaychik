using System.Threading;
using System.Threading.Tasks;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Common.Interfaces.Repositories;

namespace MusicService.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IUserRepository _users;
        private readonly IArtistRepository _artists;
        private readonly IAlbumRepository _albums;
        private readonly ITrackRepository _tracks;
        private readonly IPlaylistRepository _playlists;
        private readonly IListenHistoryRepository _listenHistories;

        public UnitOfWork(
            IUserRepository users,
            IArtistRepository artists,
            IAlbumRepository albums,
            ITrackRepository tracks,
            IPlaylistRepository playlists,
            IListenHistoryRepository listenHistories)
        {
            _users = users;
            _artists = artists;
            _albums = albums;
            _tracks = tracks;
            _playlists = playlists;
            _listenHistories = listenHistories;
        }

        public IUserRepository Users => _users;
        public IArtistRepository Artists => _artists;
        public IAlbumRepository Albums => _albums;
        public ITrackRepository Tracks => _tracks;
        public IPlaylistRepository Playlists => _playlists;
        public IListenHistoryRepository ListenHistories => _listenHistories;

        // Для файлового хранилища изменения сохраняются автоматически,
        // но этот метод можно использовать для транзакционной логики в будущем
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // В файловом хранилище каждая операция сохраняется автоматически
            // Можно добавить логику кэширования и массового сохранения при необходимости
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            // Освобождение ресурсов, если необходимо
        }
    }
}