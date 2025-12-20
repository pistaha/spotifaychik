using MediatR;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Users.Dtos;

namespace MusicService.Application.Users.Queries
{
    public record GetUsersQuery : IRequest<PagedResult<UserDto>>
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? Search { get; init; }
        public string? Country { get; init; }
    }
}
