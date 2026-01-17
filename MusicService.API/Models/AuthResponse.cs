using System;
using MusicService.Application.Users.Dtos;

namespace MusicService.API.Models
{
    public sealed class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public UserDto User { get; set; } = new();
    }
}
