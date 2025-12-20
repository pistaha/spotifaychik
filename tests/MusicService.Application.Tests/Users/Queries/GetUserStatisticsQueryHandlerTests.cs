using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Users.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Users.Queries;

public class GetUserStatisticsQueryHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPlaylistRepository> _playlistRepository = new();
    private readonly Mock<IListenHistoryRepository> _listenHistoryRepository = new();
    private readonly Mock<IArtistRepository> _artistRepository = new();
    private readonly Mock<ITrackRepository> _trackRepository = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<GetUserStatisticsQueryHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldReturnEmptyStatistics_WhenUserNotFound()
    {
        var handler = CreateHandler();
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await handler.Handle(new GetUserStatisticsQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.TotalPlaylists.Should().Be(0);
        result.TopTracks.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldAggregateListeningStats()
    {
        var userId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var artist = new Artist { Id = artistId, Name = "Artist", Genres = new List<string> { "Rock" } };
        var track = new Track { Id = Guid.NewGuid(), Title = "Track", ArtistId = artistId, Artist = artist };
        var listenHistory = new List<ListenHistory>
        {
            new()
            {
                UserId = userId,
                TrackId = track.Id,
                Track = track,
                ListenDurationSeconds = 180,
                ListenedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = userId });
        _playlistRepository.Setup(r => r.GetUserPlaylistsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Playlist> { new() });
        _listenHistoryRepository.Setup(r => r.GetUserHistoryAsync(userId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listenHistory);
        _artistRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { artist });
        _userRepository.Setup(r => r.GetUserFriendsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { new() });

        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserStatisticsQuery { UserId = userId }, CancellationToken.None);

        result.TotalPlaylists.Should().Be(1);
        result.TotalListeningTime.Should().BeGreaterThan(0);
        result.TopTracks.Should().ContainSingle();
        result.TopArtists.Should().ContainSingle();
        result.TopGenres.Should().ContainSingle().And.Contain("Rock");
        result.FollowersCount.Should().Be(1);
    }

    private GetUserStatisticsQueryHandler CreateHandler() =>
        new(_userRepository.Object, _playlistRepository.Object, _listenHistoryRepository.Object,
            _artistRepository.Object, _trackRepository.Object, _mapper, _logger.Object);
}
