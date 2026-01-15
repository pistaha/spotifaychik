using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Users.Commands
{
    public class AddFriendCommandHandler : IRequestHandler<AddFriendCommand, bool>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly ILogger<AddFriendCommandHandler> _logger;

        public AddFriendCommandHandler(
            IMusicServiceDbContext dbContext,
            ILogger<AddFriendCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(AddFriendCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding friend {FriendId} to user {UserId}", 
                request.FriendId, request.UserId);
            
            IDbContextTransaction? transaction = null;
            try
            {
                var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
                if (isInMemory)
                {
                    var user = await _dbContext.Users
                        .Include(u => u.Friends)
                        .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
                    var friend = await _dbContext.Users
                        .FirstOrDefaultAsync(u => u.Id == request.FriendId, cancellationToken);

                    if (user == null)
                    {
                        _logger.LogWarning("User {UserId} not found", request.UserId);
                        return false;
                    }

                    if (friend == null)
                    {
                        _logger.LogWarning("Friend {FriendId} not found", request.FriendId);
                        return false;
                    }

                    if (request.UserId == request.FriendId)
                    {
                        _logger.LogWarning("User cannot add themselves as a friend");
                        return false;
                    }

                    if (user.Friends.Any(f => f.Id == request.FriendId))
                    {
                        _logger.LogInformation("Users are already friends");
                        return true;
                    }

                    user.Friends.Add(friend);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Friend added successfully");
                    return true;
                }

                transaction = await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);

                var userExists = await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Id == request.UserId, cancellationToken);
                if (!userExists)
                {
                    _logger.LogWarning("User {UserId} not found", request.UserId);
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    return false;
                }

                var friendExists = await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Id == request.FriendId, cancellationToken);
                if (!friendExists)
                {
                    _logger.LogWarning("Friend {FriendId} not found", request.FriendId);
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    return false;
                }

                if (request.UserId == request.FriendId)
                {
                    _logger.LogWarning("User cannot add themselves as a friend");
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    return false;
                }

                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO user_friends (user_id, friend_id) VALUES ({request.UserId}, {request.FriendId}) ON CONFLICT DO NOTHING",
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Friend added successfully");
                return true;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                _logger.LogError(ex, "Error adding friend {FriendId} to user {UserId}",
                    request.FriendId, request.UserId);
                return false;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }
    }
}
