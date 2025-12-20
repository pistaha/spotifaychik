using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicService.Domain.Entities
{
    public class Playlist : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public bool IsPublic { get; set; }
        public bool IsCollaborative { get; set; }
        public PlaylistType Type { get; set; }
        public int FollowersCount { get; set; }
        public int TotalDurationMinutes { get; set; }
        
        // Связи
        public Guid CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public List<PlaylistTrack> PlaylistTracks { get; set; } = new();
        public List<User> Followers { get; set; } = new();

        public int TrackCount => PlaylistTracks.Count;
        
        public bool CanBeEditedBy(User user)
        {
            return CreatedById == user.Id || IsCollaborative;
        }
    }

    public enum PlaylistType
    {
        UserCreated,
        SystemGenerated,
        DailyMix,
        ReleaseRadar,
        DiscoverWeekly
    }
}