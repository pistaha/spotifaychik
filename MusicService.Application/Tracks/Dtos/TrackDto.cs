using MusicService.Application.Common.Dtos;

namespace MusicService.Application.Tracks.Dtos
{
    public class TrackDto : BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string DurationFormatted { get; set; } = string.Empty;
        public int TrackNumber { get; set; }
        public int PlayCount { get; set; }
        public int LikeCount { get; set; }
        public bool IsExplicit { get; set; }
        public bool IsLiked { get; set; }
        
        public Guid AlbumId { get; set; }
        public string AlbumTitle { get; set; } = string.Empty;
        public string? AlbumCoverImage { get; set; }
        
        public Guid ArtistId { get; set; }
        public string ArtistName { get; set; } = string.Empty;
    }

    public class CreateTrackDto
    {
        public string Title { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string? Lyrics { get; set; }
        public string? AudioFileUrl { get; set; }
        public int TrackNumber { get; set; }
        public bool IsExplicit { get; set; }
        public Guid AlbumId { get; set; }
        public Guid ArtistId { get; set; }
    }
}