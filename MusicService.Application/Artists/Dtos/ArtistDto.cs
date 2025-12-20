using System.Collections.Generic;
using MusicService.Application.Common.Dtos;

namespace MusicService.Application.Artists.Dtos
{
    public class ArtistDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string? RealName { get; set; }
        public string? Biography { get; set; }
        public string? ProfileImage { get; set; }
        public string? CoverImage { get; set; }
        public List<string> Genres { get; set; } = new();
        public string Country { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public int MonthlyListeners { get; set; }
        public int AlbumCount { get; set; }
        public int TrackCount { get; set; }
        public int FollowerCount { get; set; }
        public int YearsActive { get; set; }
    }

    public class CreateArtistDto
    {
        public string Name { get; set; } = string.Empty;
        public string? RealName { get; set; }
        public string? Biography { get; set; }
        public string? ProfileImage { get; set; }
        public string? CoverImage { get; set; }
        public List<string> Genres { get; set; } = new();
        public string Country { get; set; } = "Unknown";
        public DateTime? CareerStartDate { get; set; }
    }
}