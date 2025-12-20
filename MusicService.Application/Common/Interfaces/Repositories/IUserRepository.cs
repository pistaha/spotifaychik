using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MusicService.Domain.Entities;

namespace MusicService.Application.Common.Interfaces.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<List<User>> GetUserFriendsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> AddFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);
    Task<List<User>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);
}
