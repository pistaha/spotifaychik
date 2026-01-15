using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicService.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Country { get; set; } = "Unknown";
        public List<string> FavoriteGenres { get; set; } = new();
        public int ListenTimeMinutes { get; set; }
        public DateTime LastLogin { get; set; }
        
        // Навигационные свойства
        public List<Playlist> CreatedPlaylists { get; set; } = new();
        public List<Playlist> FollowedPlaylists { get; set; } = new();
        public List<Track> FavoriteTracks { get; set; } = new();
        public List<Artist> FollowedArtists { get; set; } = new();
        public List<Album> FavoriteAlbums { get; set; } = new();
        public List<User> Friends { get; set; } = new();
        public List<ListenHistory> ListenHistory { get; set; } = new();
        public List<UserRole> UserRoles { get; set; } = new();

        public void AddListenTime(int minutes)
        {
            ListenTimeMinutes += minutes;
            UpdateTimestamp();
        }

        public bool IsAdult()
        {
            if (!DateOfBirth.HasValue) return false;
            var age = DateTime.UtcNow.Year - DateOfBirth.Value.Year;
            if (DateTime.UtcNow < DateOfBirth.Value.AddYears(age)) age--;
            return age >= 18;
        }
    }
}
