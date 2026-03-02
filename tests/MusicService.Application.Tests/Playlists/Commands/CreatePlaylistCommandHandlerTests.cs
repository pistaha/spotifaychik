using FluentAssertions;
using Microsoft.Extensions.Logging;
using MusicService.Application.Playlists.Commands;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Application.Tests.Playlists.Commands;

public class CreatePlaylistCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreatePlaylistAndReturnDto()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        dbContext.Users.Add(new User
        {
            Id = userId,
            Username = "user",
            Email = "user@music.local",
            PasswordHash = "hash",
            DisplayName = "User",
            Country = "US",
            FavoriteGenres = new List<string>()
        });
        await dbContext.SaveChangesAsync();

        var command = new CreatePlaylistCommand
        {
            Title = "My Favorites",
            Description = "Best tracks",
            IsPublic = true,
            Type = "UserCreated",
            CreatedBy = userId
        };
        var handler = new CreatePlaylistCommandHandler(
            dbContext,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<CreatePlaylistCommandHandler>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be(command.Title);
        dbContext.Playlists.Should().ContainSingle(p => p.Title == command.Title && p.CreatedById == userId);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var command = new CreatePlaylistCommand
        {
            Title = "My Favorites",
            Type = "UserCreated",
            CreatedBy = Guid.NewGuid()
        };
        var handler = new CreatePlaylistCommandHandler(
            dbContext,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<CreatePlaylistCommandHandler>());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*User*not found*");
    }
}
