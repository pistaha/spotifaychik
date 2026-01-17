using System.Collections.Generic;
using MusicService.Application.Common.Dtos;

namespace MusicService.Application.Users.Dtos
{
    public class UserDto : BaseDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string? PhoneNumber { get; set; }
        public string Country { get; set; } = string.Empty;
        public List<string> FavoriteGenres { get; set; } = new();
        public int ListenTimeMinutes { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public bool IsActive { get; set; }
        public int PlaylistCount { get; set; }
        public int FollowingCount { get; set; }
        public int FollowerCount { get; set; }
    }

    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Country { get; set; } = "Unknown";
        public string? PhoneNumber { get; set; }
        public List<string> FavoriteGenres { get; set; } = new();
    }
}
