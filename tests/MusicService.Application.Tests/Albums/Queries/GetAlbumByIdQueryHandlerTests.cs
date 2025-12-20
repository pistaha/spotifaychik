using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Albums.Queries;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Albums.Queries;

public class GetAlbumByIdQueryHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IAlbumRepository> _albumRepository = new();

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenNotFound()
    {
        var handler = new GetAlbumByIdQueryHandler(_albumRepository.Object, _mapper);
        _albumRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Album?)null);

        var result = await handler.Handle(new GetAlbumByIdQuery { AlbumId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapAlbum()
    {
        var album = new Album { Id = Guid.NewGuid(), Title = "T", Artist = new Artist() };
        _albumRepository.Setup(r => r.GetByIdAsync(album.Id, It.IsAny<CancellationToken>())).ReturnsAsync(album);
        var handler = new GetAlbumByIdQueryHandler(_albumRepository.Object, _mapper);

        var result = await handler.Handle(new GetAlbumByIdQuery { AlbumId = album.Id }, CancellationToken.None);

        result!.Id.Should().Be(album.Id);
    }
}
