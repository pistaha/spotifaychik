using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Albums.Commands;

public class CreateAlbumCommandHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IAlbumRepository> _albumRepository = new();
    private readonly Mock<IArtistRepository> _artistRepository = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<CreateAlbumCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldCreateAlbum_WhenArtistExists()
    {
        var command = new CreateAlbumCommand { ArtistId = Guid.NewGuid(), Title = "Album", Type = "Album" };
        _artistRepository.Setup(r => r.GetByIdAsync(command.ArtistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Artist { Id = command.ArtistId });
        _albumRepository.Setup(r => r.CreateAsync(It.IsAny<Album>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Album a, CancellationToken _) => { a.Id = Guid.NewGuid(); return a; });
        var handler = new CreateAlbumCommandHandler(_albumRepository.Object, _artistRepository.Object, _mapper, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Album");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenArtistMissing()
    {
        var command = new CreateAlbumCommand { ArtistId = Guid.NewGuid(), Title = "Album", Type = "Album" };
        _artistRepository.Setup(r => r.GetByIdAsync(command.ArtistId, It.IsAny<CancellationToken>())).ReturnsAsync((Artist?)null);
        var handler = new CreateAlbumCommandHandler(_albumRepository.Object, _artistRepository.Object, _mapper, _logger.Object);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
