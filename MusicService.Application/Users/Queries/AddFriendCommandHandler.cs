using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Users.Commands
{
    public class AddFriendCommandHandler : IRequestHandler<AddFriendCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AddFriendCommandHandler> _logger;

        public AddFriendCommandHandler(
            IUserRepository userRepository,
            ILogger<AddFriendCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(AddFriendCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding friend {FriendId} to user {UserId}", 
                request.FriendId, request.UserId);
            
            try
            {
                // Проверяем, что пользователи существуют
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                var friend = await _userRepository.GetByIdAsync(request.FriendId, cancellationToken);
                
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
                
                // Проверяем, не являются ли они уже друзьями
                var friends = await _userRepository.GetUserFriendsAsync(request.UserId, cancellationToken);
                if (friends.Any(f => f.Id == request.FriendId))
                {
                    _logger.LogInformation("Users are already friends");
                    return true; // Уже друзья, считаем операцию успешной
                }
                
                // Добавляем друга
                var result = await _userRepository.AddFriendAsync(request.UserId, request.FriendId, cancellationToken);
                
                if (result)
                {
                    _logger.LogInformation("Friend added successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to add friend");
                }
                
                return result;
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