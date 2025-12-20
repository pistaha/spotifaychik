using System;
using System.Collections.Generic;

namespace MusicService.Application.Search.Dtos
{
    public class SearchResultDto
    {
        public List<ArtistSearchResultDto> Artists { get; set; } = new();
        public List<AlbumSearchResultDto> Albums { get; set; } = new();
        public List<TrackSearchResultDto> Tracks { get; set; } = new();
        public List<PlaylistSearchResultDto> Playlists { get; set; } = new();
        public List<UserSearchResultDto> Users { get; set; } = new();
        public int TotalResults { get; set; }
    }

    public class ArtistSearchResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public List<string> Genres { get; set; } = new();
        public int MonthlyListeners { get; set; }
        public double Relevance { get; set; }
    }

    public class AlbumSearchResultDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public double Relevance { get; set; }
    }

    public class TrackSearchResultDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public string AlbumTitle { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public double Relevance { get; set; }
    }

    public class PlaylistSearchResultDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public int TrackCount { get; set; }
        public int FollowersCount { get; set; }
        public double Relevance { get; set; }
    }

    public class UserSearchResultDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public double Relevance { get; set; }
    }

    public class GlobalSearchResultDto
    {
        public List<GlobalArtistDto> TopArtists { get; set; } = new();
        public List<GlobalAlbumDto> TopAlbums { get; set; } = new();
        public List<GlobalTrackDto> TopTracks { get; set; } = new();
        public List<GlobalPlaylistDto> TopPlaylists { get; set; } = new();
        public int TotalResults { get; set; }
    }

    public class GlobalArtistDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class GlobalAlbumDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string ArtistName { get; set; } = string.Empty;
    }

    public class GlobalTrackDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public int Duration { get; set; }
    }

    public class GlobalPlaylistDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string CreatorName { get; set; } = string.Empty;
    }

    public class AdvancedSearchResultDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty; // "artist", "album", "track", "playlist", "user"
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? ImageUrl { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public double RelevanceScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}