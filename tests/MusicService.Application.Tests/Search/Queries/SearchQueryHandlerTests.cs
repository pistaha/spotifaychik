using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Search.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Search.Queries;

public class SearchQueryHandlerTests
{
    private readonly Fixture _fixture;

    public SearchQueryHandlerTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_ShouldAggregateAllResultTypes_WhenTypeIsAll()
    {
        var request = new SearchQuery { Query = "Rock", Type = "all", Limit = 5 };
        var handler = CreateHandler(
            out var artistRepo,
            out var albumRepo,
            out var trackRepo,
            out var playlistRepo,
            out var userRepo,
            out var mapper,
            out var logger);

        var artist = _fixture.Build<Artist>()
            .With(a => a.Name, "Rocking Star")
            .With(a => a.Genres, new List<string> { "Rock", "Indie" })
            .Create();

        var album = _fixture.Build<Album>()
            .With(a => a.ArtistId, artist.Id)
            .With(a => a.Title, "Rock Legends")
            .Create();

        var track = _fixture.Build<Track>()
            .With(t => t.ArtistId, artist.Id)
            .With(t => t.AlbumId, album.Id)
            .With(t => t.Title, "Rock Anthem")
            .Create();

        var playlist = _fixture.Build<Playlist>()
            .With(p => p.Title, "Rock Mix")
            .With(p => p.CreatedById, Guid.NewGuid())
            .With(p => p.IsPublic, true)
            .Create();

        var user = _fixture.Build<User>()
            .With(u => u.Username, "rocker")
            .With(u => u.DisplayName, "Rock Fan")
            .Create();

        artistRepo.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { artist });
        artistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { artist });

        albumRepo.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Album> { album });
        albumRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Album> { album });

        trackRepo.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Track> { track });

        playlistRepo.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Playlist> { playlist });

        userRepo.Setup(r => r.SearchUsersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        var result = await handler.Handle(request, CancellationToken.None);

        result.TotalResults.Should().Be(5);
        result.Artists.Should().ContainSingle(a => a.Name == "Rocking Star");
        result.Albums.Should().ContainSingle(a => a.ArtistName == "Rocking Star");
        result.Tracks.Should().ContainSingle(t => t.Title == "Rock Anthem");
        result.Playlists.Should().ContainSingle(p => p.Title == "Rock Mix");
        result.Users.Should().ContainSingle(u => u.Username == "rocker");
    }

    [Fact]
    public async Task Handle_ShouldRespectTypeFilterAndLimit_WhenSearchingSpecificEntity()
    {
        var request = new SearchQuery { Query = "beat", Type = "track", Limit = 1 };
        var handler = CreateHandler(
            out var artistRepo,
            out var albumRepo,
            out var trackRepo,
            out var playlistRepo,
            out var userRepo,
            out var mapper,
            out var logger);

        var artist = new Artist { Id = Guid.NewGuid(), Name = "DJ Test" };
        var album = new Album { Id = Guid.NewGuid(), ArtistId = artist.Id, Title = "Test Album", ReleaseDate = DateTime.UtcNow };

        var tracks = Enumerable.Range(0, 3)
            .Select(i => new Track
            {
                Id = Guid.NewGuid(),
                Title = $"Beat Track {i}",
                ArtistId = artist.Id,
                AlbumId = album.Id,
                DurationSeconds = 200 + i
            })
            .ToList();

        trackRepo.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracks);

        artistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { artist });
        albumRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Album> { album });

        var result = await handler.Handle(request, CancellationToken.None);

        result.Tracks.Should().HaveCount(1);
        result.Tracks.Single().Title.Should().Be("Beat Track 0");
        result.TotalResults.Should().Be(1);

        artistRepo.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        albumRepo.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        playlistRepo.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        userRepo.Verify(r => r.SearchUsersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenRepositoryThrows()
    {
        var request = new SearchQuery { Query = "fail", Type = "artist" };
        var handler = CreateHandler(
            out var artistRepo,
            out var albumRepo,
            out var trackRepo,
            out var playlistRepo,
            out var userRepo,
            out var mapper,
            out var logger);

        artistRepo.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await handler.Handle(request, CancellationToken.None);

        result.TotalResults.Should().Be(0);
        result.Artists.Should().BeEmpty();
        result.Albums.Should().BeEmpty();
        result.Tracks.Should().BeEmpty();
        result.Playlists.Should().BeEmpty();
        result.Users.Should().BeEmpty();
    }

    private static SearchQueryHandler CreateHandler(
        out Mock<IArtistRepository> artistRepository,
        out Mock<IAlbumRepository> albumRepository,
        out Mock<ITrackRepository> trackRepository,
        out Mock<IPlaylistRepository> playlistRepository,
        out Mock<IUserRepository> userRepository,
        out Mock<IMapper> mapper,
        out Mock<ILogger<SearchQueryHandler>> logger)
    {
        artistRepository = new Mock<IArtistRepository>();
        albumRepository = new Mock<IAlbumRepository>();
        trackRepository = new Mock<ITrackRepository>();
        playlistRepository = new Mock<IPlaylistRepository>();
        userRepository = new Mock<IUserRepository>();
        mapper = new Mock<IMapper>();
        logger = new Mock<ILogger<SearchQueryHandler>>();

        return new SearchQueryHandler(
            artistRepository.Object,
            albumRepository.Object,
            trackRepository.Object,
            playlistRepository.Object,
            userRepository.Object,
            mapper.Object,
            logger.Object);
    }
}
