using MediatR;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Playlists.Queries;
using MusicService.Application.Users.Commands;
using MusicService.Application.Users.Dtos;
using MusicService.Application.Users.Queries;
using MusicService.Application.Tracks.Dtos;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserByIdQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
                
            return Ok(ApiResponse<UserDto>.SuccessResult(result, "User retrieved successfully"));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 400)]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
            [FromBody] CreateUserCommand command, 
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, 
                ApiResponse<UserDto>.SuccessResult(result, "User created successfully"));
        }

        [HttpPost("bulk")]
        [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<UserDto>>), 200)]
        public async Task<ActionResult<ApiResponse<BulkOperationResult<UserDto>>>> BulkCreateUsers(
            [FromBody] List<CreateUserCommand> commands,
            CancellationToken cancellationToken = default)
        {
            var query = new BulkCreateUsersCommand { Commands = commands };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<BulkOperationResult<UserDto>>.SuccessResult(result, "Users bulk created successfully"));
        }

        [HttpGet("{id:guid}/playlists")]
        [ProducesResponseType(typeof(ApiResponse<List<PlaylistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PlaylistDto>>>> GetUserPlaylists(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserPlaylistsByUserIdQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<PlaylistDto>>.SuccessResult(result, "User playlists retrieved successfully"));
        }

        [HttpPost("{id:guid}/friends/{friendId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<ActionResult<ApiResponse<bool>>> AddFriend(
            Guid id, 
            Guid friendId, 
            CancellationToken cancellationToken = default)
        {
            var command = new AddFriendCommand 
            { 
                UserId = id, 
                FriendId = friendId 
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(result, "Friend added successfully"));
        }

        [HttpGet("{id:guid}/statistics")]
        [ProducesResponseType(typeof(ApiResponse<UserStatisticsDto>), 200)]
        public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetUserStatistics(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserStatisticsQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<UserStatisticsDto>.SuccessResult(result, "User statistics retrieved successfully"));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? country = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUsersQuery 
            { 
                Page = page, 
                PageSize = pageSize,
                Search = search,
                Country = country
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<PagedResult<UserDto>>.SuccessResult(result, "Users retrieved successfully"));
        }
    }

}
