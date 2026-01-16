using FluentAssertions;
using MusicService.Application.Users.Queries;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Infrastructure.Tests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetUsers_ShouldFilterByCountry()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        dbContext.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                Username = "us_user",
                Email = "us@music.local",
                PasswordHash = "hash",
                DisplayName = "US User",
                Country = "US",
                FavoriteGenres = new List<string>()
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "uk_user",
                Email = "uk@music.local",
                PasswordHash = "hash",
                DisplayName = "UK User",
                Country = "UK",
                FavoriteGenres = new List<string>()
            });
        await dbContext.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(dbContext);

        var result = await handler.Handle(new GetUsersQuery { Page = 1, PageSize = 10, Country = "US" }, CancellationToken.None);

        result.Items.Should().ContainSingle(u => u.Country == "US");
    }
}
