using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Tracks.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace tests.MusicService.Application.Tests.Tracks.Queries;

public class GetTrackByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnTrackDto_WhenTrackExists()
    {
        // Arrange
        var trackId = Guid.NewGuid();
        var query = new GetTrackByIdQuery { TrackId = trackId };
        
        var track = new Track { Id = trackId, Title = "Test Track", DurationSeconds = 180 };
        var trackDto = new TrackDto { Id = trackId, Title = "Test Track" };

        var trackRepoMock = new Mock<ITrackRepository>();
        trackRepoMock.Setup(r => r.GetByIdAsync(trackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(track);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<TrackDto>(track)).Returns(trackDto);

        var handler = new GetTrackByIdQueryHandler(trackRepoMock.Object, mapperMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(trackId);
        result.Title.Should().Be("Test Track");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTrackNotFound()
    {
        // Arrange
        var trackId = Guid.NewGuid();
        var query = new GetTrackByIdQuery { TrackId = trackId };
        
        var trackRepoMock = new Mock<ITrackRepository>();
        trackRepoMock.Setup(r => r.GetByIdAsync(trackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Track?)null);

        var handler = new GetTrackByIdQueryHandler(trackRepoMock.Object, Mock.Of<IMapper>());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
