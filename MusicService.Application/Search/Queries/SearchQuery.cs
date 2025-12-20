using MediatR;
using MusicService.Application.Search.Dtos;

namespace MusicService.Application.Search.Queries
{
    public record SearchQuery : IRequest<SearchResultDto>
    {
        public string Query { get; init; } = string.Empty;
        public string? Type { get; init; } // "all", "artist", "album", "track", "playlist", "user"
        public int Limit { get; init; } = 10;
        public int Offset { get; init; } = 0;
    }
}