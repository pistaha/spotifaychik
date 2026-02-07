using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common;
using MusicService.Application.Common.Interfaces;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Users.Commands
{
    public class AddFriendCommandHandler : IRequestHandler<AddFriendCommand, AddFriendResult>
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

        public async Task<AddFriendResult> Handle(AddFriendCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding friend {FriendId} to user {UserId}", 
                request.FriendId, request.UserId);

            if (request.UserId == request.FriendId)
            {
                _logger.LogWarning("User cannot add themselves as a friend");
                return AddFriendResult.Failed();
            }
            
            var maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                IDbContextTransaction? transaction = null;
                try
                {
                    if (_dbContext is DbContext context)
                    {
                        context.ChangeTracker.Clear();
                    }

                    var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
                    var isPostgres = _dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
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
                            return AddFriendResult.UserMissing();
                        }

                        if (friend == null)
                        {
                            _logger.LogWarning("Friend {FriendId} not found", request.FriendId);
                            return AddFriendResult.FriendMissing();
                        }

                        if (user.Friends.Any(f => f.Id == request.FriendId))
                        {
                            _logger.LogInformation("Users are already friends");
                            return AddFriendResult.AlreadyFriends();
                        }

                        user.Friends.Add(friend);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Friend added successfully");
                        return AddFriendResult.Ok();
                    }

                    transaction = await _dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable, cancellationToken);

                    if (!isPostgres)
                    {
                        var user = await _dbContext.Users
                            .Include(u => u.Friends)
                            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
                        var friend = await _dbContext.Users
                            .FirstOrDefaultAsync(u => u.Id == request.FriendId, cancellationToken);
                        if (user == null)
                        {
                            _logger.LogWarning("User {UserId} not found", request.UserId);
                            await transaction.RollbackAsync(cancellationToken);
                            return AddFriendResult.UserMissing();
                        }
                        if (friend == null)
                        {
                            _logger.LogWarning("Friend {FriendId} not found", request.FriendId);
                            await transaction.RollbackAsync(cancellationToken);
                            return AddFriendResult.FriendMissing();
                        }
                        if (user.Friends.Any(f => f.Id == request.FriendId))
                        {
                            _logger.LogInformation("Users are already friends");
                            await transaction.CommitAsync(cancellationToken);
                            return AddFriendResult.AlreadyFriends();
                        }

                        user.Friends.Add(friend);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        _logger.LogInformation("Friend added successfully");
                        return AddFriendResult.Ok();
                    }

                    var existingIds = await _dbContext.Users
                        .AsNoTracking()
                        .Where(u => u.Id == request.UserId || u.Id == request.FriendId)
                        .Select(u => u.Id)
                        .ToListAsync(cancellationToken);
                    if (!existingIds.Contains(request.UserId))
                    {
                        _logger.LogWarning("User {UserId} not found", request.UserId);
                        await transaction.RollbackAsync(cancellationToken);
                        return AddFriendResult.UserMissing();
                    }
                    if (!existingIds.Contains(request.FriendId))
                    {
                        _logger.LogWarning("Friend {FriendId} not found", request.FriendId);
                        await transaction.RollbackAsync(cancellationToken);
                        return AddFriendResult.FriendMissing();
                    }

                    var rows = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO user_friends (user_id, friend_id) VALUES ({request.UserId}, {request.FriendId}) ON CONFLICT DO NOTHING",
                        cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    if (rows == 0)
                    {
                        _logger.LogInformation("Friendship already exists");
                        return AddFriendResult.AlreadyFriends();
                    }

                    _logger.LogInformation("Friend added successfully");
                    return AddFriendResult.Ok();
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    if (DatabaseErrorDetector.IsTransient(ex) && attempt < maxAttempts)
                    {
                        await DelayAsync(attempt, cancellationToken);
                        continue;
                    }

                    if (DatabaseErrorDetector.IsUniqueViolation(ex) || await FriendshipExistsAsync(request, cancellationToken))
                    {
                        _logger.LogInformation("Friendship already exists");
                        return AddFriendResult.AlreadyFriends();
                    }

                    _logger.LogError(ex, "Error adding friend {FriendId} to user {UserId}",
                        request.FriendId, request.UserId);
                    return AddFriendResult.Failed();
                }
                finally
                {
                    if (transaction != null)
                    {
                        await transaction.DisposeAsync();
                    }
                }
            }

            return AddFriendResult.Failed();
        }

        private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
        {
            var delayMs = 50 * (int)Math.Pow(2, attempt - 1);
            return Task.Delay(delayMs, cancellationToken);
        }

        private Task<bool> FriendshipExistsAsync(AddFriendCommand request, CancellationToken cancellationToken)
        {
            return _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == request.UserId)
                .SelectMany(u => u.Friends)
                .AnyAsync(f => f.Id == request.FriendId, cancellationToken);
        }
    }
}
