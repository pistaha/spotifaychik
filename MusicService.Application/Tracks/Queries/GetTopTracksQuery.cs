using MediatR;
using System.Collections.Generic;
using MusicService.Application.Tracks.Dtos;

namespace MusicService.Application.Tracks.Queries
{
    public record GetTopTracksQuery : IRequest<List<TrackDto>>
    {
        public int Count { get; init; } = 10;
        public string? TimeRange { get; init; } // "day", "week", "month", "year", "all"
        public Guid? UserId { get; init; }
    }
}
