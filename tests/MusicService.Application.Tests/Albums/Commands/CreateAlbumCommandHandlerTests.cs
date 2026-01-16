using FluentAssertions;
using MusicService.Application.Albums.Commands;
using MusicService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Albums.Commands;

public class CreateAlbumCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateAlbum_WhenArtistExists()
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

        var command = new CreateAlbumCommand
        {
            ArtistId = artistId,
            Title = "Album",
            Type = "Album",
            ReleaseDate = DateTime.UtcNow,
            Genres = new List<string> { "Rock" }
        };
        var handler = new CreateAlbumCommandHandler(dbContext, TestMapperFactory.Create(), LoggerFactory.Create(_ => { }).CreateLogger<CreateAlbumCommandHandler>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Album");
        dbContext.Albums.Should().ContainSingle(a => a.Title == "Album" && a.ArtistId == artistId);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenArtistMissing()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var command = new CreateAlbumCommand
        {
            ArtistId = Guid.NewGuid(),
            Title = "Album",
            Type = "Album",
            ReleaseDate = DateTime.UtcNow
        };
        var handler = new CreateAlbumCommandHandler(dbContext, TestMapperFactory.Create(), LoggerFactory.Create(_ => { }).CreateLogger<CreateAlbumCommandHandler>());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
