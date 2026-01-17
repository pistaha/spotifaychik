using MediatR;
using MusicService.Application.Tracks.Dtos;

namespace MusicService.Application.Tracks.Queries
{
    public record GetTrackByIdQuery : IRequest<TrackDto?>
    {
        public Guid TrackId { get; init; }
        public Guid? UserId { get; init; }
    }
}
