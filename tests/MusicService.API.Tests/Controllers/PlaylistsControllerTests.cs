using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Moq;
using MusicService.API.Controllers;
using MusicService.Application.Common;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Playlists.Commands;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Playlists.Queries;
using Xunit;
using System.Collections.Generic;
using System.Security.Claims;

namespace Tests.MusicService.API.Tests.Controllers;

public class PlaylistsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ISecurityAuditService> _auditService = new();
    private readonly Mock<IAuthorizationService> _authorizationService = new();
    private readonly PlaylistsController _controller;

    public PlaylistsControllerTests()
    {
        _authorizationService
            .Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        _controller = new PlaylistsController(_mediator.Object, _auditService.Object, _authorizationService.Object);
    }

    [Fact]
    public async Task GetPlaylist_ShouldReturnNotFound_WhenPlaylistMissing()
    {
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetUser(userId);
        _mediator.Setup(m => m.Send(It.Is<GetPlaylistByIdQuery>(q =>
                q.PlaylistId == playlistId &&
                q.UserId == userId &&
                q.IncludePrivate &&
                !q.AllowPrivateAccess),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlaylistDto?)null);

        var result = await _controller.GetPlaylist(playlistId, null, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
        ((ApiResponse<PlaylistDto>)(result.Result as NotFoundObjectResult)!.Value!).Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePlaylist_ShouldReturnCreatedAtAction()
    {
        SetUser(Guid.NewGuid());
        var playlist = new PlaylistDto { Id = Guid.NewGuid(), Title = "Road Trip" };
        _mediator.Setup(m => m.Send(It.IsAny<CreatePlaylistCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playlist);

        var result = await _controller.CreatePlaylist(new CreatePlaylistCommand(), CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = created.Value.Should().BeOfType<ApiResponse<PlaylistDto>>().Subject;
        response.Data.Should().BeEquivalentTo(playlist);
    }

    [Fact]
    public async Task GetUserPlaylists_ShouldReturnPlaylists()
    {
        var userId = Guid.NewGuid();
        var playlists = new List<PlaylistDto> { new() { Id = Guid.NewGuid() } };
        _mediator.Setup(m => m.Send(It.Is<GetUserPlaylistsQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playlists);

        var result = await _controller.GetUserPlaylists(userId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<ApiResponse<List<PlaylistDto>>>();
        _mediator.VerifyAll();
    }

    [Fact]
    public async Task GetPublicPlaylists_ShouldReturnOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetPublicPlaylistsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlaylistDto>());

        var result = await _controller.GetPublicPlaylists(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    private void SetUser(Guid userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}
