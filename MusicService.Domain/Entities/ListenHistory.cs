using System;

namespace MusicService.Domain.Entities
{
    public class ListenHistory : BaseEntity
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }
        
        public Guid TrackId { get; set; }
        public Track? Track { get; set; }
        
        public DateTime ListenedAt { get; set; }
        public int ListenDurationSeconds { get; set; }
        public string? Device { get; set; }
        public bool Completed { get; set; }
        
        public bool WasListenedToday => ListenedAt.Date == DateTime.UtcNow.Date;
    }
}