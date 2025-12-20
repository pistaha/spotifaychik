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

public class ListenHistoryRepositoryTests
{
    [Fact]
    public async Task GetUserHistoryAsync_ShouldFilterByDatesAndOrderDescending()
    {
        using var storage = new TempFileStorage();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var repository = CreateRepository(storage.FilePath, new[]
        {
            CreateEntry(userId, now.AddDays(-1)),
            CreateEntry(userId, now.AddDays(-5)),
            CreateEntry(Guid.NewGuid(), now)
        });

        var result = await repository.GetUserHistoryAsync(userId, fromDate: now.AddDays(-3));

        result.Should().HaveCount(1);
        result.Single().ListenedAt.Should().BeCloseTo(now.AddDays(-1), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetRecentlyPlayedAsync_ShouldReturnMostRecentTracks()
    {
        using var storage = new TempFileStorage();
        var userId = Guid.NewGuid();
        var trackA = new Track { Id = Guid.NewGuid(), Title = "A" };
        var trackB = new Track { Id = Guid.NewGuid(), Title = "B" };
        var trackC = new Track { Id = Guid.NewGuid(), Title = "C" };
        var repository = CreateRepository(storage.FilePath, new[]
        {
            CreateEntry(userId, DateTime.UtcNow.AddMinutes(-10), trackA),
            CreateEntry(userId, DateTime.UtcNow.AddMinutes(-5), trackB),
            CreateEntry(userId, DateTime.UtcNow.AddMinutes(-1), trackC)
        });

        var result = await repository.GetRecentlyPlayedAsync(userId, count: 2);

        result.Should().HaveCount(2);
        result.Select(t => t.Title).Should().Equal("C", "B");
    }

    [Fact]
    public async Task GetTopArtistsAsync_ShouldGroupAndSortByPlayCount()
    {
        using var storage = new TempFileStorage();
        var userId = Guid.NewGuid();
        var artistA = new Artist { Id = Guid.NewGuid(), Name = "Alpha" };
        var artistB = new Artist { Id = Guid.NewGuid(), Name = "Beta" };
        var repository = CreateRepository(storage.FilePath, new[]
        {
            CreateEntry(userId, DateTime.UtcNow.AddMinutes(-10), track: new Track { ArtistId = artistA.Id, Artist = artistA }),
            CreateEntry(userId, DateTime.UtcNow.AddMinutes(-8), track: new Track { ArtistId = artistA.Id, Artist = artistA }),
            CreateEntry(userId, DateTime.UtcNow.AddMinutes(-5), track: new Track { ArtistId = artistB.Id, Artist = artistB })
        });

        var result = await repository.GetTopArtistsAsync(userId, count: 1);

        result.Should().ContainSingle();
        result.Single().Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetTopTracksAsync_ShouldReturnTracksOrderedByPlayCount()
    {
        using var storage = new TempFileStorage();
        var userId = Guid.NewGuid();
        var trackA = new Track { Id = Guid.NewGuid(), Title = "Alpha" };
        var trackB = new Track { Id = Guid.NewGuid(), Title = "Beta" };
        var repository = CreateRepository(storage.FilePath, new[]
        {
            CreateEntry(userId, DateTime.UtcNow.AddMinutes(-10), track: trackA),
            CreateEntry(userId, DateTime.UtcNow.AddMinutes(-8), track: trackA),
            CreateEntry(userId, DateTime.UtcNow.AddMinutes(-5), track: trackB),
            CreateEntry(Guid.NewGuid(), DateTime.UtcNow, track: trackB)
        });

        var result = await repository.GetTopTracksAsync(userId, count: 2);

        result.Should().HaveCount(2);
        result.First().Title.Should().Be("Alpha");
    }

    private static ListenHistoryRepository CreateRepository(string filePath, IEnumerable<ListenHistory> entries)
    {
        var options = Options.Create(new FileStorageOptions
        {
            PrettyPrintJson = false,
            Backup = new BackupOptions { Enabled = false }
        });

        var repository = new TestableListenHistoryRepository(filePath, Mock.Of<ILogger<ListenHistoryRepository>>(), options);
        repository.Seed(entries);
        return repository;
    }

    private static ListenHistory CreateEntry(Guid userId, DateTime listenedAt, Track? track = null)
    {
        return new ListenHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TrackId = track?.Id ?? Guid.NewGuid(),
            Track = track,
            ListenedAt = listenedAt,
            CreatedAt = listenedAt,
            UpdatedAt = listenedAt
        };
    }

    private sealed class TestableListenHistoryRepository : ListenHistoryRepository
    {
        public TestableListenHistoryRepository(string filePath, ILogger<ListenHistoryRepository> logger, IOptions<FileStorageOptions> options)
            : base(filePath, logger, options)
        {
        }

        public void Seed(IEnumerable<ListenHistory> entries)
        {
            WriteAllAsync(entries.ToList(), CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
