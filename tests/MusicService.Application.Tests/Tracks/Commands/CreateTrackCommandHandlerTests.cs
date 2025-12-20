using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Tracks.Commands;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Tracks.Commands;

public class CreateTrackCommandHandlerTests
{
    private readonly Fixture _fixture;

    public CreateTrackCommandHandlerTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_ShouldCreateTrackAndReturnDto()
    {
        var command = _fixture.Build<CreateTrackCommand>()
            .With(c => c.Title, "Ocean Eyes")
            .With(c => c.DurationSeconds, 240)
            .With(c => c.TrackNumber, 3)
            .With(c => c.IsExplicit, false)
            .With(c => c.AlbumId, Guid.NewGuid())
            .With(c => c.ArtistId, Guid.NewGuid())
            .Create();

        var album = _fixture.Build<Album>()
            .With(a => a.Id, command.AlbumId)
            .Create();

        var artist = _fixture.Build<Artist>()
            .With(a => a.Id, command.ArtistId)
            .Create();

        var trackRepositoryMock = new Mock<ITrackRepository>();
        Track? persistedTrack = null;
        trackRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Track>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Track track, CancellationToken _) =>
            {
                persistedTrack = track;
                track.Id = Guid.NewGuid();
                track.CreatedAt = DateTime.UtcNow;
                track.UpdatedAt = DateTime.UtcNow;
                return track;
            });

        var albumRepositoryMock = new Mock<IAlbumRepository>();
        albumRepositoryMock
            .Setup(r => r.GetByIdAsync(command.AlbumId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        var artistRepositoryMock = new Mock<IArtistRepository>();
        artistRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ArtistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new MappingProfile())).CreateMapper();
        var loggerMock = new Mock<ILogger<CreateTrackCommandHandler>>();
        var handler = new CreateTrackCommandHandler(
            trackRepositoryMock.Object,
            albumRepositoryMock.Object,
            artistRepositoryMock.Object,
            mapper,
            loggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be(command.Title);
        result.DurationSeconds.Should().Be(command.DurationSeconds);
        result.AlbumId.Should().Be(command.AlbumId);
        result.ArtistId.Should().Be(command.ArtistId);
        persistedTrack.Should().NotBeNull();
        persistedTrack!.Title.Should().Be(command.Title);
        persistedTrack.DurationSeconds.Should().Be(command.DurationSeconds);
        trackRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Track>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenAlbumNotFound()
    {
        var command = _fixture.Build<CreateTrackCommand>()
            .With(c => c.AlbumId, Guid.NewGuid())
            .With(c => c.ArtistId, Guid.NewGuid())
            .Create();

        var trackRepositoryMock = new Mock<ITrackRepository>();
        var albumRepositoryMock = new Mock<IAlbumRepository>();
        albumRepositoryMock
            .Setup(r => r.GetByIdAsync(command.AlbumId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Album?)null);

        var artistRepositoryMock = new Mock<IArtistRepository>();
        artistRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ArtistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Artist { Id = command.ArtistId });

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new MappingProfile())).CreateMapper();
        var loggerMock = new Mock<ILogger<CreateTrackCommandHandler>>();

        var handler = new CreateTrackCommandHandler(
            trackRepositoryMock.Object,
            albumRepositoryMock.Object,
            artistRepositoryMock.Object,
            mapper,
            loggerMock.Object);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Album*not found*");
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenArtistNotFound()
    {
        var command = _fixture.Build<CreateTrackCommand>()
            .With(c => c.AlbumId, Guid.NewGuid())
            .With(c => c.ArtistId, Guid.NewGuid())
            .Create();

        var trackRepositoryMock = new Mock<ITrackRepository>();
        var albumRepositoryMock = new Mock<IAlbumRepository>();
        albumRepositoryMock
            .Setup(r => r.GetByIdAsync(command.AlbumId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Album { Id = command.AlbumId });

        var artistRepositoryMock = new Mock<IArtistRepository>();
        artistRepositoryMock
            .Setup(r => r.GetByIdAsync(command.ArtistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new MappingProfile())).CreateMapper();
        var loggerMock = new Mock<ILogger<CreateTrackCommandHandler>>();

        var handler = new CreateTrackCommandHandler(
            trackRepositoryMock.Object,
            albumRepositoryMock.Object,
            artistRepositoryMock.Object,
            mapper,
            loggerMock.Object);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Artist*not found*");
    }
}
