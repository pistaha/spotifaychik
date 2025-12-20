using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Artists.Commands;

public class CreateArtistCommandHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IArtistRepository> _artistRepository = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<CreateArtistCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldCreateArtistAndReturnDto()
    {
        _artistRepository.Setup(r => r.CreateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist a, CancellationToken _) => { a.Id = Guid.NewGuid(); return a; });
        var handler = new CreateArtistCommandHandler(_artistRepository.Object, _mapper, _logger.Object);

        var result = await handler.Handle(new CreateArtistCommand { Name = "Artist" }, CancellationToken.None);

        result.Should().BeOfType<ArtistDto>();
        _artistRepository.Verify(r => r.CreateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
