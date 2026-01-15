using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces;
using System;
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
            
            try
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
                
                if (user.Id == friend.Id)
                {
                    _logger.LogWarning("User cannot add themselves as a friend");
                    return false;
                }
                
                if (user.Friends.Any(f => f.Id == request.FriendId))
                {
                    _logger.LogInformation("Users are already friends");
                    return true; // Уже друзья, считаем операцию успешной
                }
                
                user.Friends.Add(friend);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Friend added successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding friend {FriendId} to user {UserId}", 
                    request.FriendId, request.UserId);
                return false;
            }
        }
    }
}
