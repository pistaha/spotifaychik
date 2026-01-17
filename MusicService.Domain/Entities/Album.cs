using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicService.Domain.Entities
{
    public class Album : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public DateTime ReleaseDate { get; set; }
        public AlbumType Type { get; set; }
        public List<string> Genres { get; set; } = new();
        public int TotalDurationMinutes { get; set; }
        public int TrackCount => Tracks.Count;
        
        // Связи
        public Guid ArtistId { get; set; }
        public Artist? Artist { get; set; }
        public Guid? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public List<Track> Tracks { get; set; } = new();
        public List<User> AddedByUsers { get; set; } = new();

        public bool IsRecentRelease()
        {
            return (DateTime.UtcNow - ReleaseDate).TotalDays <= 30;
        }

        public bool IsSingle => Type == AlbumType.Single;
    }

    public enum AlbumType
    {
        Album,
        EP,
        Single,
        Compilation,
        Live
    }
}
