using MediatR;
using MusicService.Application.Search.Dtos;

namespace MusicService.Application.Search.Queries
{
    public record GlobalSearchQuery : IRequest<GlobalSearchResultDto>
    {
        public string Query { get; init; } = string.Empty;
        public int Limit { get; init; } = 5;
    }
}