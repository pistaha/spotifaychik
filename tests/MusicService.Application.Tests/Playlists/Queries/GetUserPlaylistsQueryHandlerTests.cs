using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Playlists.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Playlists.Queries;

public class GetUserPlaylistsQueryHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IPlaylistRepository> _playlistRepository = new();

    [Fact]
    public async Task Handle_ShouldFilterPrivatePlaylists_WhenIncludePrivateIsFalse()
    {
        var playlists = new List<Playlist>
        {
            new() { Id = Guid.NewGuid(), IsPublic = true, PlaylistTracks = new List<PlaylistTrack>() },
            new() { Id = Guid.NewGuid(), IsPublic = false, PlaylistTracks = new List<PlaylistTrack>() }
        };
        _playlistRepository.Setup(r => r.GetUserPlaylistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playlists);
        var handler = new GetUserPlaylistsQueryHandler(_playlistRepository.Object, _mapper);

        var result = await handler.Handle(new GetUserPlaylistsQuery { UserId = Guid.NewGuid(), IncludePrivate = false }, CancellationToken.None);

        result.Should().HaveCount(1);
        result.All(p => p.IsPublic).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnAllPlaylists_WhenIncludePrivateIsTrue()
    {
        _playlistRepository.Setup(r => r.GetUserPlaylistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Playlist> { new() { IsPublic = false, PlaylistTracks = new List<PlaylistTrack>() } });
        var handler = new GetUserPlaylistsQueryHandler(_playlistRepository.Object, _mapper);

        var result = await handler.Handle(new GetUserPlaylistsQuery { UserId = Guid.NewGuid(), IncludePrivate = true }, CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().IsPublic.Should().BeFalse();
    }
}
