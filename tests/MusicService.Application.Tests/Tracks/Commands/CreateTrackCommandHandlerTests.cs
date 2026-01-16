using FluentAssertions;
using Microsoft.Extensions.Logging;
using MusicService.Application.Tracks.Commands;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Application.Tests.Tracks.Commands;

public class CreateTrackCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateTrackAndReturnDto()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        dbContext.Artists.Add(new Artist
        {
            Id = artistId,
            Name = "Artist",
            Country = "US",
            Genres = new List<string>()
        });
        dbContext.Albums.Add(new Album
        {
            Id = albumId,
            ArtistId = artistId,
            Title = "Album",
            ReleaseDate = DateTime.UtcNow,
            Type = AlbumType.Album,
            Genres = new List<string>()
        });
        await dbContext.SaveChangesAsync();

        var command = new CreateTrackCommand
        {
            Title = "Ocean Eyes",
            DurationSeconds = 240,
            TrackNumber = 3,
            IsExplicit = false,
            AlbumId = albumId,
            ArtistId = artistId
        };
        var handler = new CreateTrackCommandHandler(
            dbContext,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<CreateTrackCommandHandler>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be(command.Title);
        dbContext.Tracks.Should().ContainSingle(t => t.Title == command.Title && t.AlbumId == albumId);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenAlbumNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artistId = Guid.NewGuid();
        dbContext.Artists.Add(new Artist
        {
            Id = artistId,
            Name = "Artist",
            Country = "US",
            Genres = new List<string>()
        });
        await dbContext.SaveChangesAsync();

        var command = new CreateTrackCommand
        {
            Title = "No Album",
            DurationSeconds = 240,
            TrackNumber = 1,
            AlbumId = Guid.NewGuid(),
            ArtistId = artistId
        };
        var handler = new CreateTrackCommandHandler(
            dbContext,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<CreateTrackCommandHandler>());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Album*not found*");
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenArtistNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        dbContext.Artists.Add(new Artist
        {
            Id = artistId,
            Name = "Artist",
            Country = "US",
            Genres = new List<string>()
        });
        dbContext.Albums.Add(new Album
        {
            Id = albumId,
            ArtistId = artistId,
            Title = "Album",
            ReleaseDate = DateTime.UtcNow,
            Type = AlbumType.Album,
            Genres = new List<string>()
        });
        await dbContext.SaveChangesAsync();

        var command = new CreateTrackCommand
        {
            Title = "No Artist",
            DurationSeconds = 240,
            TrackNumber = 1,
            AlbumId = albumId,
            ArtistId = Guid.NewGuid()
        };
        var handler = new CreateTrackCommandHandler(
            dbContext,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<CreateTrackCommandHandler>());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Artist*not found*");
    }
}
