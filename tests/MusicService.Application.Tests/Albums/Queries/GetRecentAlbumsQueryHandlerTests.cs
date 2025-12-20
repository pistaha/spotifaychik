using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Albums.Queries;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Albums.Queries;

public class GetRecentAlbumsQueryHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IAlbumRepository> _albumRepository = new();

    [Fact]
    public async Task Handle_ShouldRequestRecentAlbumsForGivenDays()
    {
        _albumRepository.Setup(r => r.GetRecentReleasesAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Album> { new() { Id = Guid.NewGuid(), Artist = new Artist() } });
        var handler = new GetRecentAlbumsQueryHandler(_albumRepository.Object, _mapper);

        var result = await handler.Handle(new GetRecentAlbumsQuery { Days = 7 }, CancellationToken.None);

        result.Should().HaveCount(1);
        _albumRepository.Verify(r => r.GetRecentReleasesAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }
}
