using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MusicService.API.Controllers;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Artists.Queries;
using MusicService.Application.Common;
using Xunit;

namespace Tests.MusicService.API.Tests.Controllers;

public class ArtistsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly ArtistsController _controller;

    public ArtistsControllerTests()
    {
        _controller = new ArtistsController(_mediator.Object);
    }

    [Fact]
    public async Task GetArtist_ShouldReturnNotFound_WhenArtistMissing()
    {
        var artistId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetArtistByIdQuery>(q => q.ArtistId == artistId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArtistDto?)null);

        var result = await _controller.GetArtist(artistId, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
        ((ApiResponse<ArtistDto>)(result.Result as NotFoundObjectResult)!.Value!).Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateArtist_ShouldReturnCreatedAtAction()
    {
        var dto = new ArtistDto { Id = Guid.NewGuid(), Name = "New Artist" };
        _mediator.Setup(m => m.Send(It.IsAny<CreateArtistCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.CreateArtist(new CreateArtistCommand(), CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = created.Value.Should().BeOfType<ApiResponse<ArtistDto>>().Subject;
        response.Data.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetTopArtists_ShouldRespectCountParameter()
    {
        var artists = new List<ArtistDto> { new() { Id = Guid.NewGuid() } };
        _mediator.Setup(m => m.Send(It.Is<GetTopArtistsQuery>(q => q.Count == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(artists);

        var result = await _controller.GetTopArtists(5, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mediator.VerifyAll();
    }

    [Fact]
    public async Task GetArtistsByGenre_ShouldReturnOk()
    {
        var genre = "rock";
        var artists = new List<ArtistDto> { new() { Id = Guid.NewGuid(), Name = "Artist" } };
        _mediator.Setup(m => m.Send(It.Is<GetArtistsByGenreQuery>(q => q.Genre == genre), It.IsAny<CancellationToken>()))
            .ReturnsAsync(artists);

        var result = await _controller.GetArtistsByGenre(genre, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<ApiResponse<List<ArtistDto>>>();
    }
}
