using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Configuration;
using MusicService.Infrastructure.Repositories;
using Tests.TestUtilities;
using Xunit;

namespace Tests.MusicService.Infrastructure.Tests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task FindByEmailAndUsername_ShouldReturnMatchingUsers()
    {
        using var storage = new TempFileStorage();
        var user = CreateUser(u =>
        {
            u.Email = "test@example.com";
            u.Username = "tester";
        });

        var repository = CreateRepository(storage.FilePath, new[] { user });

        var byEmail = await repository.FindByEmailAsync("TEST@example.com");
        var byUsername = await repository.FindByUsernameAsync("Tester");

        byEmail.Should().NotBeNull();
        byUsername.Should().NotBeNull();
        byEmail!.Id.Should().Be(user.Id);
        byUsername!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task ExistsChecks_ShouldRespectCaseInsensitivity()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath, new[]
        {
            CreateUser(u =>
            {
                u.Email = "unique@mail.com";
                u.Username = "UniqueUser";
            })
        });

        (await repository.ExistsByEmailAsync("UNIQUE@mail.com")).Should().BeTrue();
        (await repository.ExistsByUsernameAsync("uniqueuser")).Should().BeTrue();
        (await repository.ExistsByEmailAsync("nope@mail.com")).Should().BeFalse();
    }

    [Fact]
    public async Task AddFriendAsync_ShouldPersistFriendship_WhenUsersExist()
    {
        using var storage = new TempFileStorage();
        var userA = CreateUser(u => u.Username = "alice");
        var userB = CreateUser(u => u.Username = "bob");
        var repository = CreateRepository(storage.FilePath, new[] { userA, userB });

        var added = await repository.AddFriendAsync(userA.Id, userB.Id);

        added.Should().BeTrue();
        var updatedUser = await repository.GetByIdAsync(userA.Id);
        updatedUser!.Friends.Should().ContainSingle(f => f.Id == userB.Id);
    }

    [Fact]
    public async Task AddFriendAsync_ShouldReturnFalse_WhenUsersMissingOrDuplicate()
    {
        using var storage = new TempFileStorage();
        var user = CreateUser(u => u.Username = "solo");
        var repository = CreateRepository(storage.FilePath, new[] { user });

        var missingFriend = await repository.AddFriendAsync(user.Id, Guid.NewGuid());
        missingFriend.Should().BeFalse();

        var duplicate = await repository.AddFriendAsync(user.Id, user.Id);
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task SearchUsersAsync_ShouldMatchOnNameAndEmail()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath, new[]
        {
            CreateUser(u =>
            {
                u.Username = "devguru";
                u.DisplayName = "Dev Guru";
                u.Email = "guru@example.com";
            }),
            CreateUser(u => u.Username = "listener")
        });

        var results = await repository.SearchUsersAsync("guru");

        results.Should().ContainSingle(u => u.Username == "devguru");
    }

    private static TestableUserRepository CreateRepository(string filePath, IEnumerable<User> users)
    {
        var options = Options.Create(new FileStorageOptions
        {
            PrettyPrintJson = false,
            Backup = new BackupOptions { Enabled = false }
        });

        var repo = new TestableUserRepository(filePath, Mock.Of<ILogger<UserRepository>>(), options);
        repo.Seed(users);
        return repo;
    }

    private static User CreateUser(Action<User> setup)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "user",
            Email = "user@example.com",
            DisplayName = "User",
            Friends = new List<User>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        setup(user);
        return user;
    }

    private sealed class TestableUserRepository : UserRepository
    {
        public TestableUserRepository(string filePath, ILogger<UserRepository> logger, IOptions<FileStorageOptions> options)
            : base(filePath, logger, options)
        {
        }

        public void Seed(IEnumerable<User> users)
        {
            WriteAllAsync(users.ToList(), CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
