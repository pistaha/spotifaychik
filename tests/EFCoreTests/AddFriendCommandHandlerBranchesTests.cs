using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicService.Application.Users.Commands;
using MusicService.Application.Users.Queries;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.EFCoreTests
{
    public class AddFriendCommandHandlerBranchesTests
    {
        private static MusicServiceDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MusicServiceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new MusicServiceDbContext(options);
        }

        [Fact]
        public async Task Handle_ShouldReturnUserNotFound_WhenUserMissing()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var handler = new AddFriendCommandHandler(dbContext, NullLogger<AddFriendCommandHandler>.Instance);

            var result = await handler.Handle(new AddFriendCommand
            {
                UserId = Guid.NewGuid(),
                FriendId = Guid.NewGuid()
            }, CancellationToken.None);

            result.Status.Should().Be(AddFriendStatus.UserNotFound);
        }

        [Fact]
        public async Task Handle_ShouldReturnFriendNotFound_WhenFriendMissing()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "user",
                Email = "user@music.local",
                PasswordHash = "hash",
                DisplayName = "User",
                Country = "US"
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var handler = new AddFriendCommandHandler(dbContext, NullLogger<AddFriendCommandHandler>.Instance);
            var result = await handler.Handle(new AddFriendCommand
            {
                UserId = user.Id,
                FriendId = Guid.NewGuid()
            }, CancellationToken.None);

            result.Status.Should().Be(AddFriendStatus.FriendNotFound);
        }

        [Fact]
        public async Task Handle_ShouldReturnError_WhenUserAddsSelf()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "user",
                Email = "user@music.local",
                PasswordHash = "hash",
                DisplayName = "User",
                Country = "US"
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var handler = new AddFriendCommandHandler(dbContext, NullLogger<AddFriendCommandHandler>.Instance);
            var result = await handler.Handle(new AddFriendCommand
            {
                UserId = user.Id,
                FriendId = user.Id
            }, CancellationToken.None);

            result.Status.Should().Be(AddFriendStatus.Error);
        }

        [Fact]
        public async Task Handle_ShouldReturnAlreadyFriends_WhenFriendshipExists()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "user",
                Email = "user@music.local",
                PasswordHash = "hash",
                DisplayName = "User",
                Country = "US"
            };
            var friend = new User
            {
                Id = Guid.NewGuid(),
                Username = "friend",
                Email = "friend@music.local",
                PasswordHash = "hash",
                DisplayName = "Friend",
                Country = "US"
            };
            user.Friends.Add(friend);
            dbContext.Users.AddRange(user, friend);
            await dbContext.SaveChangesAsync();

            var handler = new AddFriendCommandHandler(dbContext, NullLogger<AddFriendCommandHandler>.Instance);
            var result = await handler.Handle(new AddFriendCommand
            {
                UserId = user.Id,
                FriendId = friend.Id
            }, CancellationToken.None);

            result.Status.Should().Be(AddFriendStatus.AlreadyFriends);
        }

        [Fact]
        public async Task Handle_ShouldUseInMemoryBranch_WhenProviderIsInMemory()
        {
            var dbContext = CreateInMemoryDbContext();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "user",
                Email = "user@music.local",
                PasswordHash = "hash",
                DisplayName = "User",
                Country = "US"
            };
            var friend = new User
            {
                Id = Guid.NewGuid(),
                Username = "friend",
                Email = "friend@music.local",
                PasswordHash = "hash",
                DisplayName = "Friend",
                Country = "US"
            };
            user.Friends.Add(friend);
            dbContext.Users.AddRange(user, friend);
            await dbContext.SaveChangesAsync();

            var handler = new AddFriendCommandHandler(dbContext, NullLogger<AddFriendCommandHandler>.Instance);
            var result = await handler.Handle(new AddFriendCommand
            {
                UserId = user.Id,
                FriendId = friend.Id
            }, CancellationToken.None);

            result.Status.Should().Be(AddFriendStatus.AlreadyFriends);
        }
    }
}
