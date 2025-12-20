using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MusicService.API.Controllers;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Playlists.Queries;
using MusicService.Application.Users.Commands;
using MusicService.Application.Users.Dtos;
using MusicService.Application.Users.Queries;
using Xunit;

namespace Tests.MusicService.API.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_mediator.Object);
    }

    [Fact]
    public async Task GetUser_ShouldReturnNotFound_WhenUserMissing()
    {
        var userId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetUserByIdQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        var result = await _controller.GetUser(userId, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
        ((ApiResponse<UserDto>)(result.Result as NotFoundObjectResult)!.Value!).Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreatedAtAction()
    {
        var dto = new UserDto { Id = Guid.NewGuid(), Username = "test" };
        _mediator.Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.CreateUser(new CreateUserCommand(), CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UsersController.GetUser));
        var response = created.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        response.Data.Should().Be(dto);
    }

    [Fact]
    public async Task BulkCreateUsers_ShouldWrapBulkResult()
    {
        var commands = new List<CreateUserCommand> { new(), new() };
        var bulkResult = new BulkOperationResult<UserDto>
        {
            Items = new List<BulkOperationItem<UserDto>> { new() { Data = new UserDto(), Success = true } }
        };
        _mediator.Setup(m => m.Send(It.Is<BulkCreateUsersCommand>(c => c.Commands == commands), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.BulkCreateUsers(commands, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<ApiResponse<BulkOperationResult<UserDto>>>();
        _mediator.VerifyAll();
    }

    [Fact]
    public async Task AddFriend_ShouldReturnSuccessResponse()
    {
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<AddFriendCommand>(c => c.UserId == userId && c.FriendId == friendId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.AddFriend(userId, friendId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        response.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnPagedResult()
    {
        var paged = new PagedResult<UserDto>(new List<UserDto>(), totalCount: 0, pageNumber: 1, pageSize: 10);
        _mediator.Setup(m => m.Send(It.Is<GetUsersQuery>(q => q.Page == 1 && q.Country == "US"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _controller.GetUsers(page: 1, pageSize: 10, search: null, country: "US", cancellationToken: CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<ApiResponse<PagedResult<UserDto>>>();
    }

    [Fact]
    public async Task GetUserPlaylists_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetUserPlaylistsByUserIdQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlaylistDto>());

        var result = await _controller.GetUserPlaylists(userId, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
