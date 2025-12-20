using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Playlists.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Playlists.Queries;

public class GetPlaylistByIdQueryHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IPlaylistRepository> _playlistRepository = new();

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenPlaylistNotFound()
    {
        var handler = new GetPlaylistByIdQueryHandler(_playlistRepository.Object, _mapper);
        var query = new GetPlaylistByIdQuery { PlaylistId = Guid.NewGuid() };
        _playlistRepository.Setup(r => r.GetByIdAsync(query.PlaylistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Playlist?)null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldSetIsFollowingFlag_WhenUserIsFollower()
    {
        var followerId = Guid.NewGuid();
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            Title = "Chill",
            Followers = new List<User> { new() { Id = followerId } },
            PlaylistTracks = new List<PlaylistTrack>()
        };
        var handler = new GetPlaylistByIdQueryHandler(_playlistRepository.Object, _mapper);
        _playlistRepository.Setup(r => r.GetByIdAsync(playlist.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(playlist);

        var result = await handler.Handle(new GetPlaylistByIdQuery { PlaylistId = playlist.Id, UserId = followerId }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsFollowing.Should().BeTrue();
        result.Should().BeOfType<PlaylistDto>();
    }
}
