using FluentAssertions;
using Microsoft.Extensions.Logging;
using MusicService.Application.Users.Commands;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Application.Tests.Users.Commands;

public class AddFriendCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnUserMissing_WhenUserNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new AddFriendCommandHandler(
            dbContext,
            LoggerFactory.Create(_ => { }).CreateLogger<AddFriendCommandHandler>());

        var result = await handler.Handle(new AddFriendCommand { UserId = Guid.NewGuid(), FriendId = Guid.NewGuid() }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Status.Should().Be(AddFriendStatus.UserNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlreadyFriends_WhenFriendshipExists()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "user",
            Email = "user@music.local",
            PasswordHash = "hash",
            DisplayName = "User",
            Country = "US",
            FavoriteGenres = new List<string>()
        };
        var friend = new User
        {
            Id = friendId,
            Username = "friend",
            Email = "friend@music.local",
            PasswordHash = "hash",
            DisplayName = "Friend",
            Country = "US",
            FavoriteGenres = new List<string>()
        };
        user.Friends.Add(friend);
        dbContext.Users.AddRange(user, friend);
        await dbContext.SaveChangesAsync();

        var handler = new AddFriendCommandHandler(
            dbContext,
            LoggerFactory.Create(_ => { }).CreateLogger<AddFriendCommandHandler>());

        var result = await handler.Handle(new AddFriendCommand { UserId = userId, FriendId = friendId }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Status.Should().Be(AddFriendStatus.AlreadyFriends);
    }
}
