using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MusicService.API.Controllers;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Search.Dtos;
using MusicService.Application.Search.Queries;
using Xunit;

namespace Tests.MusicService.API.Tests.Controllers;

public class SearchControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly SearchController _controller;

    public SearchControllerTests()
    {
        _controller = new SearchController(_mediator.Object);
    }

    [Fact]
    public async Task Search_ShouldPassQueryParametersToMediator()
    {
        var expected = new SearchResultDto();
        _mediator.Setup(m => m.Send(It.Is<SearchQuery>(q =>
                q.Query == "beatles" && q.Type == "artist" && q.Limit == 5 && q.Offset == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Search("beatles", "artist", 5, 2, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<SearchResultDto>>().Subject;
        response.Data.Should().Be(expected);
        _mediator.VerifyAll();
    }

    [Fact]
    public async Task AdvancedSearch_ShouldWrapResultInApiResponse()
    {
        var paged = new PagedResult<AdvancedSearchResultDto>(new List<AdvancedSearchResultDto>(), totalCount: 0, pageNumber: 1, pageSize: 10);
        _mediator.Setup(m => m.Send(It.IsAny<AdvancedSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _controller.AdvancedSearch(new AdvancedPaginationRequest(), CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<ApiResponse<PagedResult<AdvancedSearchResultDto>>>();
    }

    [Fact]
    public async Task GlobalSearch_ShouldRespectLimit()
    {
        var expected = new GlobalSearchResultDto();
        _mediator.Setup(m => m.Send(It.Is<GlobalSearchQuery>(q => q.Query == "rock" && q.Limit == 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GlobalSearch("rock", 3, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<ApiResponse<GlobalSearchResultDto>>();
        _mediator.VerifyAll();
    }
}
