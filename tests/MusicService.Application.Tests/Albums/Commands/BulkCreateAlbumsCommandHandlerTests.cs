using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Albums.Commands;

public class BulkCreateAlbumsCommandHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IAlbumRepository> _albumRepository = new();
    private readonly Mock<IArtistRepository> _artistRepository = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<BulkCreateAlbumsCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldQueueValidAlbumsAndCreate()
    {
        var artistId = Guid.NewGuid();
        var command = new BulkCreateAlbumsCommand
        {
            Commands = new List<CreateAlbumCommand> { new() { ArtistId = artistId, Title = "A", Type = "Album" } }
        };
        _artistRepository.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>())).ReturnsAsync(new Artist { Id = artistId });
        _albumRepository.Setup(r => r.CreateAsync(It.IsAny<Album>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Album a, CancellationToken _) => { a.Id = Guid.NewGuid(); return a; });
        var handler = new BulkCreateAlbumsCommandHandler(_albumRepository.Object, _artistRepository.Object, _mapper, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.SuccessfulCount.Should().Be(1);
        result.Items.Should().AllSatisfy(i => i.Success.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_ShouldSkipWhenArtistMissing()
    {
        var artistId = Guid.NewGuid();
        var command = new BulkCreateAlbumsCommand
        {
            Commands = new List<CreateAlbumCommand> { new() { ArtistId = artistId, Title = "A", Type = "Album" } }
        };
        _artistRepository.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>())).ReturnsAsync((Artist?)null);
        var handler = new BulkCreateAlbumsCommandHandler(_albumRepository.Object, _artistRepository.Object, _mapper, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.SuccessfulCount.Should().Be(0);
        result.Items.Should().ContainSingle(i => !i.Success);
    }
}
