using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Tracks.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Tracks.Queries;

public class GetTopTracksQueryHandlerTests
{
    private readonly Mock<ITrackRepository> _trackRepository = new();
    private readonly Mock<IListenHistoryRepository> _listenHistoryRepository = new();
    private readonly IMapper _mapper;

    public GetTopTracksQueryHandlerTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile(new MappingProfile())).CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldUseListenHistory_WhenTimeRangeSpecified()
    {
        var trackIdA = Guid.NewGuid();
        var trackIdB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _listenHistoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ListenHistory>
            {
                new() { TrackId = trackIdA, ListenedAt = now.AddDays(-1) },
                new() { TrackId = trackIdA, ListenedAt = now.AddDays(-2) },
                new() { TrackId = trackIdB, ListenedAt = now.AddDays(-1) }
            });

        _trackRepository.Setup(r => r.GetByIdAsync(trackIdA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Track { Id = trackIdA, Title = "Track A" });
        _trackRepository.Setup(r => r.GetByIdAsync(trackIdB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Track { Id = trackIdB, Title = "Track B" });

        var handler = new GetTopTracksQueryHandler(
            _trackRepository.Object,
            _listenHistoryRepository.Object,
            _mapper);

        var result = await handler.Handle(new GetTopTracksQuery { Count = 5, TimeRange = "week" }, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(t => t.Title).Should().Equal("Track A", "Track B");
        _trackRepository.Verify(r => r.GetTopTracksAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _listenHistoryRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallTrackRepository_WhenTimeRangeNotSpecified()
    {
        var topTracks = new List<Track>
        {
            new() { Id = Guid.NewGuid(), Title = "Alpha", PlayCount = 100 },
            new() { Id = Guid.NewGuid(), Title = "Beta", PlayCount = 50 }
        };

        _trackRepository.Setup(r => r.GetTopTracksAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topTracks);

        var handler = new GetTopTracksQueryHandler(
            _trackRepository.Object,
            _listenHistoryRepository.Object,
            _mapper);

        var result = await handler.Handle(new GetTopTracksQuery { Count = 2, TimeRange = "all" }, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(t => t.Title).Should().Equal("Alpha", "Beta");
        _trackRepository.Verify(r => r.GetTopTracksAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        _listenHistoryRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
