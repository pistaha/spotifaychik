using FluentAssertions;
using MusicService.Application.Artists.Queries;
using MusicService.Domain.Entities;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Artists.Queries;

public class GetArtistByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new GetArtistByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetArtistByIdQuery { ArtistId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapArtist()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artist = new Artist
        {
            Id = Guid.NewGuid(),
            Name = "Artist",
            Country = "US",
            Genres = new List<string>()
        };
        dbContext.Artists.Add(artist);
        await dbContext.SaveChangesAsync();
        var handler = new GetArtistByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetArtistByIdQuery { ArtistId = artist.Id }, CancellationToken.None);

        result!.Id.Should().Be(artist.Id);
    }
}
