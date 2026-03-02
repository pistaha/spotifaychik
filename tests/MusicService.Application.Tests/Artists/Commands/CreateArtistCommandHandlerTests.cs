using FluentAssertions;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Artists.Dtos;
using MusicService.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Artists.Commands;

public class CreateArtistCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateArtistAndReturnDto()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var creatorId = Guid.NewGuid();
        dbContext.Users.Add(new User
        {
            Id = creatorId,
            Username = "creator",
            Email = "creator@test.com",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            DisplayName = "Creator",
            Country = "US",
            FavoriteGenres = new List<string>()
        });
        await dbContext.SaveChangesAsync();
        var handler = new CreateArtistCommandHandler(
            dbContext,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<CreateArtistCommandHandler>());

        var result = await handler.Handle(new CreateArtistCommand { Name = "Artist", CreatedById = creatorId }, CancellationToken.None);

        result.Should().BeOfType<ArtistDto>();
        dbContext.Artists.Should().ContainSingle(a => a.Name == "Artist");
    }
}
