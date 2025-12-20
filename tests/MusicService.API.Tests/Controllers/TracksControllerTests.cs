using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MusicService.API.Controllers;
using MusicService.Application.Common;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Tracks.Queries;
using Xunit;

namespace Tests.MusicService.API.Tests.Controllers;

public class TracksControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly TracksController _controller;

    public TracksControllerTests()
    {
        _controller = new TracksController(_mediator.Object);
    }

    [Fact]
    public async Task GetTrack_ShouldReturnNotFound_WhenTrackMissing()
    {
        var trackId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetTrackByIdQuery>(q => q.TrackId == trackId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackDto?)null);

        var result = await _controller.GetTrack(trackId, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
        ((ApiResponse<TrackDto>)(result.Result as NotFoundObjectResult)!.Value!).Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetTracksByAlbum_ShouldReturnOkResult()
    {
        var albumId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetTracksByAlbumQuery>(q => q.AlbumId == albumId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackDto>());

        var result = await _controller.GetTracksByAlbum(albumId, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mediator.VerifyAll();
    }

    [Fact]
    public async Task GetTopTracks_ShouldReturnRequestedCount()
    {
        var tracks = new List<TrackDto> { new() { Id = Guid.NewGuid() } };
        _mediator.Setup(m => m.Send(It.Is<GetTopTracksQuery>(q => q.Count == 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracks);

        var result = await _controller.GetTopTracks(3, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<ApiResponse<List<TrackDto>>>();
    }
}
