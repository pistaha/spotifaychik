using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicService.Domain.Entities
{
    public class Track : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string? Lyrics { get; set; }
        public string? AudioFileUrl { get; set; }
        public int TrackNumber { get; set; }
        public int PlayCount { get; set; }
        public int LikeCount { get; set; }
        public bool IsExplicit { get; set; }
        public decimal PopularityScore { get; set; }
        
        // Связи
        public Guid AlbumId { get; set; }
        public Album? Album { get; set; }
        public Guid ArtistId { get; set; }
        public Artist? Artist { get; set; }
        public Guid? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public List<PlaylistTrack> PlaylistTracks { get; set; } = new();
        public List<User> LikedByUsers { get; set; } = new();
        public List<ListenHistory> ListenHistory { get; set; } = new();

        public string DurationFormatted
        {
            get
            {
                var span = TimeSpan.FromSeconds(DurationSeconds);
                return $"{(int)span.TotalMinutes}:{span.Seconds:00}";
            }
        }

        public void IncrementPlayCount()
        {
            PlayCount++;
            UpdateTimestamp();
        }
    }
}
