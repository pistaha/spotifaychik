using FluentAssertions;
using MusicService.Application.Users.Queries;
using MusicService.Domain.Entities;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Users.Queries;

public class GetUserByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenUserMissing()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new GetUserByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetUserByIdQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapUser_WhenFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "user",
            Email = "user@music.local",
            PasswordHash = "hash",
            DisplayName = "User",
            Country = "US",
            FavoriteGenres = new List<string>()
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new GetUserByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetUserByIdQuery { UserId = user.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }
}
