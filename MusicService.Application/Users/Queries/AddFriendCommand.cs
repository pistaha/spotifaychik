using MediatR;
using System;

namespace MusicService.Application.Users.Commands
{
    public record AddFriendCommand : IRequest<bool>
    {
        public Guid UserId { get; init; }
        public Guid FriendId { get; init; }
    }
}