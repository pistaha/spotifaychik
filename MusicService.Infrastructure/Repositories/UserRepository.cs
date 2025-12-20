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
    public class UserRepository : FileStorageRepository<User>, IUserRepository
    {
        public UserRepository(
            string filePath,
            ILogger<UserRepository> logger,
            IOptions<FileStorageOptions> options) : base(filePath, logger, options)
        {
        }

        public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var users = await GetAllAsync(cancellationToken);
            return users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var users = await GetAllAsync(cancellationToken);
            return users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var users = await GetAllAsync(cancellationToken);
            return users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var users = await GetAllAsync(cancellationToken);
            return users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<User>> GetUserFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var users = await GetAllAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.Id == userId);
            return user?.Friends ?? new List<User>();
        }

        public async Task<bool> AddFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            var users = await GetAllAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.Id == userId);
            var friend = users.FirstOrDefault(u => u.Id == friendId);

            if (user == null || friend == null || user.Id == friend.Id)
                return false;

            if (user.Friends.Any(f => f.Id == friendId))
                return false;

            user.Friends.Add(friend);
            await WriteAllAsync(users, cancellationToken);
            return true;
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var users = await GetAllAsync(cancellationToken);
            return users
                .Where(u => u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           u.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
