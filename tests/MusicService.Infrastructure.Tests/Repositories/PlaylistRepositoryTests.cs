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

public class PlaylistRepositoryTests
{
    [Fact]
    public async Task GetUserPlaylistsAsync_ShouldReturnOwnedAndFollowedPlaylists()
    {
        using var storage = new TempFileStorage();
        var userId = Guid.NewGuid();
        var playlists = new[]
        {
            CreatePlaylist(p =>
            {
                p.Id = Guid.NewGuid();
                p.CreatedById = userId;
                p.Title = "My Mix";
            }),
            CreatePlaylist(p =>
            {
                p.Id = Guid.NewGuid();
                p.CreatedById = Guid.NewGuid();
                p.IsPublic = true;
                p.Followers = new List<User> { new User { Id = userId, Username = "fan" } };
                p.Title = "Community Blend";
            }),
            CreatePlaylist(p =>
            {
                p.Id = Guid.NewGuid();
                p.CreatedById = Guid.NewGuid();
                p.IsPublic = false;
                p.Title = "Private";
            })
        };

        var repository = CreateRepository(storage.FilePath, playlists);

        var result = await repository.GetUserPlaylistsAsync(userId);

        result.Should().HaveCount(2);
        result.Select(p => p.Title).Should().Contain(new[] { "My Mix", "Community Blend" });
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnPublicPlaylistsMatchingTerm()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath, new[]
        {
            CreatePlaylist(p =>
            {
                p.Title = "Morning Boost";
                p.Description = "Start your day";
                p.IsPublic = true;
            }),
            CreatePlaylist(p =>
            {
                p.Title = "Night Owl";
                p.Description = "Late coding";
                p.IsPublic = false;
            }),
            CreatePlaylist(p =>
            {
                p.Title = "Coding Flow";
                p.Description = "best focus tracks";
                p.IsPublic = true;
            })
        });

        var result = await repository.SearchAsync("coding");

        result.Should().HaveCount(1);
        result.Single().Title.Should().Be("Coding Flow");
    }

    [Fact]
    public async Task AddTrackToPlaylistAsync_ShouldRespectPermissions()
    {
        using var storage = new TempFileStorage();
        var ownerId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var playlist = CreatePlaylist(p =>
        {
            p.Id = Guid.NewGuid();
            p.CreatedById = ownerId;
            p.IsCollaborative = true;
        });

        var repository = CreateRepository(storage.FilePath, new[] { playlist });

        var resultOwner = await repository.AddTrackToPlaylistAsync(playlist.Id, Guid.NewGuid(), ownerId);
        var resultCollaborator = await repository.AddTrackToPlaylistAsync(playlist.Id, Guid.NewGuid(), collaboratorId);
        var resultUnauthorized = await repository.AddTrackToPlaylistAsync(Guid.NewGuid(), Guid.NewGuid(), collaboratorId);

        resultOwner.Should().BeTrue();
        resultCollaborator.Should().BeTrue();
        resultUnauthorized.Should().BeFalse();
    }

    [Fact]
    public async Task FollowPlaylistAsync_ShouldIncrementFollowersCount_ForPublicPlaylists()
    {
        using var storage = new TempFileStorage();
        var playlist = CreatePlaylist(p =>
        {
            p.Id = Guid.NewGuid();
            p.IsPublic = true;
            p.FollowersCount = 1;
        });

        var repository = CreateRepository(storage.FilePath, new[] { playlist });

        var success = await repository.FollowPlaylistAsync(playlist.Id, Guid.NewGuid());
        var updated = await repository.GetByIdAsync(playlist.Id);

        success.Should().BeTrue();
        updated!.FollowersCount.Should().Be(2);
    }

    [Fact]
    public async Task FollowPlaylistAsync_ShouldFail_WhenPlaylistIsPrivateOrMissing()
    {
        using var storage = new TempFileStorage();
        var privatePlaylist = CreatePlaylist(p =>
        {
            p.Id = Guid.NewGuid();
            p.IsPublic = false;
        });

        var repository = CreateRepository(storage.FilePath, new[] { privatePlaylist });

        var resultPrivate = await repository.FollowPlaylistAsync(privatePlaylist.Id, Guid.NewGuid());
        var resultMissing = await repository.FollowPlaylistAsync(Guid.NewGuid(), Guid.NewGuid());

        resultPrivate.Should().BeFalse();
        resultMissing.Should().BeFalse();
    }

    private static TestablePlaylistRepository CreateRepository(string path, IEnumerable<Playlist> playlists)
    {
        var options = Options.Create(new FileStorageOptions
        {
            PrettyPrintJson = false,
            Backup = new BackupOptions { Enabled = false }
        });

        var repo = new TestablePlaylistRepository(path, Mock.Of<ILogger<PlaylistRepository>>(), options);
        repo.Seed(playlists);
        return repo;
    }

    private static Playlist CreatePlaylist(Action<Playlist> setup)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            Title = "Untitled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPublic = true,
            Followers = new List<User>(),
            PlaylistTracks = new List<PlaylistTrack>()
        };
        setup(playlist);
        return playlist;
    }

    private sealed class TestablePlaylistRepository : PlaylistRepository
    {
        public TestablePlaylistRepository(string filePath, ILogger<PlaylistRepository> logger, IOptions<FileStorageOptions> options)
            : base(filePath, logger, options)
        {
        }

        public void Seed(IEnumerable<Playlist> playlists)
        {
            var list = playlists.ToList();
            WriteAllAsync(list, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
