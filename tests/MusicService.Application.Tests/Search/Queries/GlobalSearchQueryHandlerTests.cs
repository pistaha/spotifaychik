using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Search.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Search.Queries;

public class GlobalSearchQueryHandlerTests
{
    private readonly Mock<IArtistRepository> _artistRepository = new();
    private readonly Mock<IAlbumRepository> _albumRepository = new();
    private readonly Mock<ITrackRepository> _trackRepository = new();
    private readonly Mock<IPlaylistRepository> _playlistRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<GlobalSearchQueryHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldAggregateLimitedResultsAndSetTotal()
    {
        var artists = new List<Artist> { new() { Id = Guid.NewGuid(), Name = "A", MonthlyListeners = 10, Genres = new() } };
        var albums = new List<Album> { new() { Id = Guid.NewGuid(), Title = "Album", ArtistId = artists[0].Id, ReleaseDate = DateTime.UtcNow, Artist = artists[0] } };
        var tracks = new List<Track> { new() { Id = Guid.NewGuid(), Title = "Track", ArtistId = artists[0].Id, PlayCount = 5, Artist = artists[0] } };
        var playlists = new List<Playlist> { new() { Id = Guid.NewGuid(), Title = "Mix", FollowersCount = 100, CreatedById = Guid.NewGuid() } };
        var users = new List<User> { new() { Id = playlists[0].CreatedById, Username = "creator" } };

        _artistRepository.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(artists);
        _artistRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(artists);
        _albumRepository.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(albums);
        _trackRepository.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(tracks);
        _playlistRepository.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(playlists);
        _userRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var handler = new GlobalSearchQueryHandler(_artistRepository.Object, _albumRepository.Object, _trackRepository.Object,
            _playlistRepository.Object, _userRepository.Object, _logger.Object);

        var result = await handler.Handle(new GlobalSearchQuery { Query = "mix", Limit = 1 }, CancellationToken.None);

        result.TopArtists.Should().HaveCount(1);
        result.TotalResults.Should().Be(4);
        result.TopPlaylists.Single().CreatorName.Should().Be("creator");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenRepositoriesFail()
    {
        _artistRepository.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        var handler = new GlobalSearchQueryHandler(_artistRepository.Object, _albumRepository.Object, _trackRepository.Object,
            _playlistRepository.Object, _userRepository.Object, _logger.Object);

        var result = await handler.Handle(new GlobalSearchQuery { Query = "fail", Limit = 2 }, CancellationToken.None);

        result.TotalResults.Should().Be(0);
        result.TopArtists.Should().BeEmpty();
    }
}
