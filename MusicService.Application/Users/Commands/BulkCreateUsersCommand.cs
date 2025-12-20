using System.Collections.Generic;
using MediatR;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Users.Dtos;

namespace MusicService.Application.Users.Commands
{
    public record BulkCreateUsersCommand : IRequest<BulkOperationResult<UserDto>>
    {
        public List<CreateUserCommand> Commands { get; init; } = new();
    }
}
