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

public class TrackRepositoryTests
{
    [Fact]
    public async Task GetTracksByAlbumAsync_ShouldFilterAndOrderByTrackNumber()
    {
        using var storage = new TempFileStorage();
        var albumId = Guid.NewGuid();
        var repository = CreateRepository(storage.FilePath);
        await repository.SeedAsync(new[]
        {
            CreateTrack(albumId: albumId, trackNumber: 2, title: "Second"),
            CreateTrack(albumId: albumId, trackNumber: 1, title: "First"),
            CreateTrack(albumId: Guid.NewGuid(), trackNumber: 3, title: "Other")
        });

        var result = await repository.GetTracksByAlbumAsync(albumId);

        result.Should().HaveCount(2);
        result.Select(t => t.TrackNumber).Should().Equal(1, 2);
        result.Select(t => t.Title).Should().Equal("First", "Second");
    }

    [Fact]
    public async Task SearchAsync_ShouldMatchTitleAndLyricsIgnoringCase()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath);
        await repository.SeedAsync(new[]
        {
            CreateTrack(title: "Midnight City", lyrics: "This is a neon skyline"),
            CreateTrack(title: "Sunrise Run", lyrics: "Chasing the beat and the wind"),
            CreateTrack(title: "Silent Night")
        });

        var result = await repository.SearchAsync("NeOn");

        result.Should().ContainSingle();
        result.Single().Title.Should().Be("Midnight City");

        var resultByLyrics = await repository.SearchAsync("beat");
        resultByLyrics.Should().ContainSingle();
        resultByLyrics.Single().Title.Should().Be("Sunrise Run");
    }

    [Fact]
    public async Task GetTopTracksAsync_ShouldReturnSortedSubsetByPlayCount()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath);
        await repository.SeedAsync(new[]
        {
            CreateTrack(title: "Track A", playCount: 10),
            CreateTrack(title: "Track B", playCount: 50),
            CreateTrack(title: "Track C", playCount: 20)
        });

        var result = await repository.GetTopTracksAsync(2);

        result.Should().HaveCount(2);
        result.Select(t => t.Title).Should().Equal("Track B", "Track C");
    }

    [Fact]
    public async Task IncrementPlayCountAsync_ShouldPersistIncrements()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath);
        var track = CreateTrack(title: "Loop", playCount: 5);
        await repository.SeedAsync(new[] { track });

        await repository.IncrementPlayCountAsync(track.Id);
        await repository.IncrementPlayCountAsync(Guid.NewGuid()); // unknown id should be ignored

        var fetched = await repository.GetByIdAsync(track.Id);
        fetched!.PlayCount.Should().Be(6);
    }

    private static TestableTrackRepository CreateRepository(string filePath)
    {
        var logger = Mock.Of<ILogger<TrackRepository>>();
        var options = Options.Create(new FileStorageOptions
        {
            PrettyPrintJson = false,
            Backup = new BackupOptions { Enabled = false }
        });

        return new TestableTrackRepository(filePath, logger, options);
    }

    private static Track CreateTrack(Guid? albumId = null, Guid? artistId = null, int trackNumber = 1, string title = "", string? lyrics = null, int playCount = 0)
    {
        return new Track
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(title) ? $"Track-{Guid.NewGuid():N}" : title,
            AlbumId = albumId ?? Guid.NewGuid(),
            ArtistId = artistId ?? Guid.NewGuid(),
            TrackNumber = trackNumber,
            Lyrics = lyrics,
            PlayCount = playCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private sealed class TestableTrackRepository : TrackRepository
    {
        public TestableTrackRepository(string filePath, ILogger<TrackRepository> logger, IOptions<FileStorageOptions> options)
            : base(filePath, logger, options)
        {
        }

        public Task SeedAsync(IEnumerable<Track> tracks, CancellationToken cancellationToken = default)
            => WriteAllAsync(tracks.ToList(), cancellationToken);
    }
}
