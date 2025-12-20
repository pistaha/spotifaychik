using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Artists.Queries;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Artists.Queries;

public class GetArtistByIdQueryHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IArtistRepository> _artistRepository = new();

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenNotFound()
    {
        var handler = new GetArtistByIdQueryHandler(_artistRepository.Object, _mapper);
        _artistRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        var result = await handler.Handle(new GetArtistByIdQuery { ArtistId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapArtist()
    {
        var artist = new Artist { Id = Guid.NewGuid(), Name = "Artist" };
        _artistRepository.Setup(r => r.GetByIdAsync(artist.Id, It.IsAny<CancellationToken>())).ReturnsAsync(artist);
        var handler = new GetArtistByIdQueryHandler(_artistRepository.Object, _mapper);

        var result = await handler.Handle(new GetArtistByIdQuery { ArtistId = artist.Id }, CancellationToken.None);

        result!.Id.Should().Be(artist.Id);
    }
}
