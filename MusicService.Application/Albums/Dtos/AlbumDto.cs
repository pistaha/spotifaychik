using System;
using System.Collections.Generic;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Tracks.Dtos;

namespace MusicService.Application.Albums.Dtos
{
    public class AlbumDto : BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public List<string> Genres { get; set; } = new();
        public int TotalDurationMinutes { get; set; }
        public int TrackCount { get; set; }
        
        public Guid ArtistId { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public List<TrackDto> Tracks { get; set; } = new();
        
        public bool IsRecentRelease { get; set; }
    }

    public class CreateAlbumDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public List<string> Genres { get; set; } = new();
        public Guid ArtistId { get; set; }
    }
}