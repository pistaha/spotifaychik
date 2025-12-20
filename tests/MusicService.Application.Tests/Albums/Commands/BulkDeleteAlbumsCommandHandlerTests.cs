using FluentAssertions;
using Moq;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Albums.Commands;

public class BulkDeleteAlbumsCommandHandlerTests
{
    private readonly Mock<IAlbumRepository> _albumRepository = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<BulkDeleteAlbumsCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldMarkMissingAlbumAsFailed()
    {
        var id = Guid.NewGuid();
        _albumRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Album?)null);
        var handler = new BulkDeleteAlbumsCommandHandler(_albumRepository.Object, _logger.Object);

        var result = await handler.Handle(new BulkDeleteAlbumsCommand { AlbumIds = new List<Guid> { id } }, CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.Id == id && !i.Success);
        result.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldDeleteAlbumAndReturnSuccess()
    {
        var id = Guid.NewGuid();
        _albumRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new Album { Id = id });
        _albumRepository.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new BulkDeleteAlbumsCommandHandler(_albumRepository.Object, _logger.Object);

        var result = await handler.Handle(new BulkDeleteAlbumsCommand { AlbumIds = new List<Guid> { id } }, CancellationToken.None);

        result.SuccessfulCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Success);
    }
}
