using FluentAssertions;
using MusicService.Application.Albums.Commands;
using MusicService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Albums.Commands;

public class BulkDeleteAlbumsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMarkMissingAlbumAsFailed()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var id = Guid.NewGuid();
        var handler = new BulkDeleteAlbumsCommandHandler(
            dbContext,
            LoggerFactory.Create(_ => { }).CreateLogger<BulkDeleteAlbumsCommandHandler>());

        var result = await handler.Handle(new BulkDeleteAlbumsCommand { AlbumIds = new List<Guid> { id } }, CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.Id == id && !i.Success);
        result.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldDeleteAlbumAndReturnSuccess()
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
        var id = Guid.NewGuid();
        dbContext.Albums.Add(new Album
        {
            Id = id,
            Title = "Album",
            ReleaseDate = DateTime.UtcNow,
            Type = AlbumType.Album,
            ArtistId = artistId,
            Genres = new List<string>()
        });
        await dbContext.SaveChangesAsync();
        var handler = new BulkDeleteAlbumsCommandHandler(
            dbContext,
            LoggerFactory.Create(_ => { }).CreateLogger<BulkDeleteAlbumsCommandHandler>());

        var result = await handler.Handle(new BulkDeleteAlbumsCommand { AlbumIds = new List<Guid> { id } }, CancellationToken.None);

        result.SuccessfulCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Success);
    }
}
