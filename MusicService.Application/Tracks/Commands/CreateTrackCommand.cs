using MediatR;
using MusicService.Application.Tracks.Dtos;

namespace MusicService.Application.Tracks.Commands
{
    public record CreateTrackCommand : IRequest<TrackDto>
    {
        public string Title { get; init; } = string.Empty;
        public int DurationSeconds { get; init; }
        public string? Lyrics { get; init; }
        public string? AudioFileUrl { get; init; }
        public int TrackNumber { get; init; }
        public bool IsExplicit { get; init; }
        public Guid AlbumId { get; init; }
        public Guid ArtistId { get; init; }
    }
}