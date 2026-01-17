using MediatR;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Search.Dtos;
using MusicService.Application.Search.Queries;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SearchController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<SearchResultDto>), 200)]
        public async Task<ActionResult<ApiResponse<SearchResultDto>>> Search(
            [FromQuery] string query,
            [FromQuery] string? type = null,
            [FromQuery] int limit = 10,
            [FromQuery] int offset = 0,
            CancellationToken cancellationToken = default)
        {
            var searchQuery = new SearchQuery 
            { 
                Query = query, 
                Type = type,
                Limit = limit,
                Offset = offset
            };
            
            var result = await _mediator.Send(searchQuery, cancellationToken);
            return Ok(ApiResponse<SearchResultDto>.SuccessResult(result, "Search completed successfully"));
        }

        [HttpGet("advanced")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AdvancedSearchResultDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<AdvancedSearchResultDto>>>> AdvancedSearch(
            [FromQuery] AdvancedPaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = new AdvancedSearchQuery { Request = request };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<PagedResult<AdvancedSearchResultDto>>.SuccessResult(result, "Advanced search completed successfully"));
        }

        [HttpGet("global")]
        [ProducesResponseType(typeof(ApiResponse<GlobalSearchResultDto>), 200)]
        public async Task<ActionResult<ApiResponse<GlobalSearchResultDto>>> GlobalSearch(
            [FromQuery] string query,
            [FromQuery] int limit = 5,
            CancellationToken cancellationToken = default)
        {
            var searchQuery = new GlobalSearchQuery 
            { 
                Query = query, 
                Limit = limit
            };
            
            var result = await _mediator.Send(searchQuery, cancellationToken);
            return Ok(ApiResponse<GlobalSearchResultDto>.SuccessResult(result, "Global search completed successfully"));
        }
    }
}
