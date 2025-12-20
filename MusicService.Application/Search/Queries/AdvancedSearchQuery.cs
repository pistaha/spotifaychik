using MediatR;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Search.Dtos;

namespace MusicService.Application.Search.Queries
{
    public record AdvancedSearchQuery : IRequest<PagedResult<AdvancedSearchResultDto>>
    {
        public AdvancedPaginationRequest Request { get; init; } = new();
    }
}