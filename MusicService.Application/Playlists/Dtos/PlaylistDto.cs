using System.Collections.Generic;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Tracks.Dtos;

namespace MusicService.Application.Playlists.Dtos
{
    public class PlaylistDto : BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public bool IsPublic { get; set; }
        public bool IsCollaborative { get; set; }
        public string Type { get; set; } = string.Empty;
        public int FollowersCount { get; set; }
        public int TotalDurationMinutes { get; set; }
        public int TrackCount { get; set; }
        
        public Guid CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public List<TrackDto> Tracks { get; set; } = new();
        public bool IsFollowing { get; set; }
    }

    public class CreatePlaylistDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public bool IsPublic { get; set; } = true;
        public bool IsCollaborative { get; set; } = false;
        public string Type { get; set; } = "UserCreated";
        public Guid CreatedBy { get; set; }
    }
}
