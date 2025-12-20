using MediatR;
using MusicService.Application.Users.Dtos;

namespace MusicService.Application.Users.Queries
{
    public record GetUserByIdQuery : IRequest<UserDto?>
    {
        public Guid UserId { get; init; }
    }
}