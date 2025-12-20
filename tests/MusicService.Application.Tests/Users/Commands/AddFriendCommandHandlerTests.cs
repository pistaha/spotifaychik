using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Users.Commands;
using MusicService.Application.Users.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Users.Commands;

public class AddFriendCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AddFriendCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenUserNotFound()
    {
        var handler = new AddFriendCommandHandler(_userRepository.Object, _logger.Object);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await handler.Handle(new AddFriendCommand { UserId = Guid.NewGuid(), FriendId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenAlreadyFriends()
    {
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = userId });
        _userRepository.Setup(r => r.GetByIdAsync(friendId, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = friendId });
        _userRepository.Setup(r => r.GetUserFriendsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { new() { Id = friendId } });
        var handler = new AddFriendCommandHandler(_userRepository.Object, _logger.Object);

        var result = await handler.Handle(new AddFriendCommand { UserId = userId, FriendId = friendId }, CancellationToken.None);

        result.Should().BeTrue();
    }
}
