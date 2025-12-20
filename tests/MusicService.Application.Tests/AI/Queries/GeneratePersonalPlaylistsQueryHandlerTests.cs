using FluentAssertions;
using Moq;
using MusicService.Application.AI.Queries;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Playlists.Dtos;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.AI.Queries;

public class GeneratePersonalPlaylistsQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserNotFound()
    {
        var handler = new GeneratePersonalPlaylistsQueryHandler(_userRepository.Object);
        var query = new GeneratePersonalPlaylistsQuery { UserId = Guid.NewGuid(), Count = 3 };
        _userRepository.Setup(r => r.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldGeneratePlaylists_ForFavoriteGenresUpToRequestedCount()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FavoriteGenres = new List<string> { "Rock", "Pop", "Indie" }
        };
        var handler = new GeneratePersonalPlaylistsQueryHandler(_userRepository.Object);
        var query = new GeneratePersonalPlaylistsQuery { UserId = user.Id, Count = 2 };
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(p => p.Title).Should().Contain("Rock Classics Mix").And.Contain("Top Pop Hits");
        result.All(p => p.CreatedById == user.Id && p.Type == "SystemGenerated").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldUseFallbackTitle_ForUnknownGenres()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FavoriteGenres = new List<string> { "Folk" }
        };
        var handler = new GeneratePersonalPlaylistsQueryHandler(_userRepository.Object);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await handler.Handle(new GeneratePersonalPlaylistsQuery
        {
            UserId = user.Id,
            Count = 5
        }, CancellationToken.None);

        result.Should().ContainSingle();
        var playlist = result.Single();
        playlist.Title.Should().Be("My Folk Mix");
        playlist.Description.Should().NotBeNull();
        playlist.Description!.Should().ContainEquivalentOf("Folk");
        playlist.Should().BeOfType<PlaylistDto>();
    }
}
