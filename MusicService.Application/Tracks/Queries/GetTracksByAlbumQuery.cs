using MediatR;
using System;
using System.Collections.Generic;
using MusicService.Application.Tracks.Dtos;

namespace MusicService.Application.Tracks.Queries
{
    public record GetTracksByAlbumQuery : IRequest<List<TrackDto>>
    {
        public Guid AlbumId { get; init; }
        public bool SortByTrackNumber { get; init; } = true;
        public Guid? UserId { get; init; }
    }
}
