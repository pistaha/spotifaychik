using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Playlists.Commands;
using MusicService.Domain.Entities;
using Xunit;

namespace tests.MusicService.Application.Tests.Playlists.Commands;

public class CreatePlaylistCommandHandlerTests
{
    private readonly Fixture _fixture;

    public CreatePlaylistCommandHandlerTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_ShouldCreatePlaylistAndReturnDto()
    {
        // Arrange
        var command = _fixture.Build<CreatePlaylistCommand>()
            .With(c => c.Title, "My Favorites")
            .With(c => c.Description, "Best tracks")
            .With(c => c.IsPublic, true)
            .With(c => c.Type, "UserCreated")
            .With(c => c.CreatedBy, Guid.NewGuid())
            .Create();

        var user = _fixture.Build<User>()
            .With(u => u.Id, command.CreatedBy)
            .Create();

        var playlistRepoMock = new Mock<IPlaylistRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        var mapper = new Mock<IMapper>();
        var logger = new Mock<ILogger<CreatePlaylistCommandHandler>>();

        userRepoMock.Setup(r => r.GetByIdAsync(command.CreatedBy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Playlist? savedPlaylist = null;
        playlistRepoMock.Setup(r => r.CreateAsync(It.IsAny<Playlist>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Playlist p, CancellationToken _) =>
            {
                savedPlaylist = p;
                p.Id = Guid.NewGuid();
                return p;
            });

        var handler = new CreatePlaylistCommandHandler(
            playlistRepoMock.Object,
            userRepoMock.Object,
            mapper.Object,
            logger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        savedPlaylist.Should().NotBeNull();
        savedPlaylist!.Title.Should().Be(command.Title);
        playlistRepoMock.Verify(r => r.CreateAsync(It.IsAny<Playlist>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var command = _fixture.Build<CreatePlaylistCommand>()
            .With(c => c.CreatedBy, Guid.NewGuid())
            .Create();
        var playlistRepoMock = new Mock<IPlaylistRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(r => r.GetByIdAsync(command.CreatedBy, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new CreatePlaylistCommandHandler(
            playlistRepoMock.Object,
            userRepoMock.Object,
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<CreatePlaylistCommandHandler>>());

        // Act & Assert
        await handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*User*not found*");
    }
}
