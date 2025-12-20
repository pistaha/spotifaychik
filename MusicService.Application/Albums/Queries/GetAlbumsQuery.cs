using MediatR;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Dtos;

namespace MusicService.Application.Albums.Queries
{
    public record GetAlbumsQuery : IRequest<PagedResult<AlbumDto>>
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? Search { get; init; }
        public string? Genre { get; init; }
        public string? SortBy { get; init; }
        public string? SortOrder { get; init; } = "desc";
    }
}
