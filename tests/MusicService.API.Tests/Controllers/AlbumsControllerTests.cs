using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MusicService.API.Controllers;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Albums.Queries;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using Xunit;

namespace Tests.MusicService.API.Tests.Controllers;

public class AlbumsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly AlbumsController _controller;

    public AlbumsControllerTests()
    {
        _controller = new AlbumsController(_mediator.Object);
    }

    [Fact]
    public async Task GetAlbum_ShouldReturnNotFound_WhenAlbumMissing()
    {
        var albumId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetAlbumByIdQuery>(q => q.AlbumId == albumId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlbumDto?)null);

        var result = await _controller.GetAlbum(albumId, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var response = (result.Result as NotFoundObjectResult)!.Value.Should().BeOfType<ApiResponse<AlbumDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAlbum_ShouldReturnCreatedAtAction()
    {
        var album = new AlbumDto { Id = Guid.NewGuid(), Title = "Test Album" };
        _mediator.Setup(m => m.Send(It.IsAny<CreateAlbumCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        var result = await _controller.CreateAlbum(new CreateAlbumCommand(), CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = created.Value.Should().BeOfType<ApiResponse<AlbumDto>>().Subject;
        response.Data.Should().BeEquivalentTo(album);
        created.ActionName.Should().Be(nameof(AlbumsController.GetAlbum));
    }

    [Fact]
    public async Task BulkCreateAlbums_ShouldPassCommandsToMediator()
    {
        var commands = new List<CreateAlbumCommand> { new(), new() };
        var bulkResult = new BulkOperationResult<AlbumDto>
        {
            Items = new List<BulkOperationItem<AlbumDto>> { new() { Data = new AlbumDto { Id = Guid.NewGuid() }, Success = true } }
        };
        _mediator.Setup(m => m.Send(It.Is<BulkCreateAlbumsCommand>(c => c.Commands == commands), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var response = await _controller.BulkCreateAlbums(commands, CancellationToken.None);

        response.Result.Should().BeOfType<OkObjectResult>();
        _mediator.VerifyAll();
    }

    [Fact]
    public async Task GetAlbums_ShouldReturnPagedResult()
    {
        var pagedResult = new PagedResult<AlbumDto>(new List<AlbumDto>(), totalCount: 0, pageNumber: 2, pageSize: 10);
        _mediator.Setup(m => m.Send(It.Is<GetAlbumsQuery>(q => q.Page == 2 && q.Search == "rock"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var actionResult = await _controller.GetAlbums(page: 2, pageSize: 10, search: "rock", genre: null, sortBy: "CreatedAt", sortOrder: "desc", cancellationToken: CancellationToken.None);

        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var response = (actionResult.Result as OkObjectResult)!.Value.Should().BeOfType<ApiResponse<PagedResult<AlbumDto>>>().Subject;
        response.Data.Should().Be(pagedResult);
    }
}
