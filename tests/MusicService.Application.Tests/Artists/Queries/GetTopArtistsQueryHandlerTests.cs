using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Artists.Queries;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Artists.Queries;

public class GetTopArtistsQueryHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IArtistRepository> _artistRepository = new();

    [Fact]
    public async Task Handle_ShouldReturnMappedArtists()
    {
        var artists = new List<Artist> { new() { Id = Guid.NewGuid(), Name = "Top" } };
        _artistRepository.Setup(r => r.GetTopArtistsAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(artists);
        var handler = new GetTopArtistsQueryHandler(_artistRepository.Object, _mapper);

        var result = await handler.Handle(new GetTopArtistsQuery { Count = 3 }, CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Name.Should().Be("Top");
    }
}
