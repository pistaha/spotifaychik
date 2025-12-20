using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Infrastructure.Repositories;
using MusicService.Infrastructure.Security;
using System.IO;

namespace MusicService.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Конфигурация файлового хранилища
            services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
            
            // Сервисы безопасности
            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            
            // Регистрация репозиториев с использованием фабрик для инъекции пути к файлам
            RegisterFileBasedRepositories(services, configuration);
            
            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        private static void RegisterFileBasedRepositories(IServiceCollection services, IConfiguration configuration)
        {
            // Получаем путь к директории данных из конфигурации
            var dataDirectory = configuration["FileStorage:DataDirectory"] ?? "Data";
            
            // Создаем директорию, если она не существует
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            // Регистрируем репозитории с указанием путей к файлам
            services.AddScoped<IUserRepository>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<UserRepository>>();
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>();
                return new UserRepository(Path.Combine(dataDirectory, "users.json"), logger, options);
            });
            
            services.AddScoped<IArtistRepository>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ArtistRepository>>();
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>();
                return new ArtistRepository(Path.Combine(dataDirectory, "artists.json"), logger, options);
            });
            
            services.AddScoped<IAlbumRepository>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<AlbumRepository>>();
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>();
                return new AlbumRepository(Path.Combine(dataDirectory, "albums.json"), logger, options);
            });
            
            services.AddScoped<ITrackRepository>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<TrackRepository>>();
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>();
                return new TrackRepository(Path.Combine(dataDirectory, "tracks.json"), logger, options);
            });
            
            services.AddScoped<IPlaylistRepository>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<PlaylistRepository>>();
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>();
                return new PlaylistRepository(Path.Combine(dataDirectory, "playlists.json"), logger, options);
            });
            
            services.AddScoped<IListenHistoryRepository>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ListenHistoryRepository>>();
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>();
                return new ListenHistoryRepository(Path.Combine(dataDirectory, "listen-history.json"), logger, options);
            });
        }
    }
}
