using System;

namespace MusicService.Domain.Entities
{
    public class PlaylistTrack : BaseEntity
    {
        public Guid PlaylistId { get; set; }
        public Playlist? Playlist { get; set; }
        
        public Guid TrackId { get; set; }
        public Track? Track { get; set; }
        
        public int Position { get; set; }
        public DateTime AddedAt { get; set; }
        public Guid? AddedById { get; set; }
        public User? AddedBy { get; set; }
    }
}
