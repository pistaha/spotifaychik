using FluentAssertions;
using MusicService.Application.Albums.Commands;
using MusicService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Albums.Commands;

public class BulkCreateAlbumsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldQueueValidAlbumsAndCreate()
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

        var command = new BulkCreateAlbumsCommand
        {
            Commands = new List<CreateAlbumCommand>
            {
                new()
                {
                    ArtistId = artistId,
                    Title = "A",
                    Type = "Album",
                    ReleaseDate = DateTime.UtcNow,
                    Genres = new List<string> { "Rock" }
                }
            }
        };
        var handler = new BulkCreateAlbumsCommandHandler(
            dbContext,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<BulkCreateAlbumsCommandHandler>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.SuccessfulCount.Should().Be(1);
        result.Items.Should().AllSatisfy(i => i.Success.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_ShouldSkipWhenArtistMissing()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artistId = Guid.NewGuid();
        var command = new BulkCreateAlbumsCommand
        {
            Commands = new List<CreateAlbumCommand>
            {
                new()
                {
                    ArtistId = artistId,
                    Title = "A",
                    Type = "Album",
                    ReleaseDate = DateTime.UtcNow
                }
            }
        };
        var handler = new BulkCreateAlbumsCommandHandler(
            dbContext,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<BulkCreateAlbumsCommandHandler>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.SuccessfulCount.Should().Be(0);
        result.Items.Should().ContainSingle(i => !i.Success);
    }
}
