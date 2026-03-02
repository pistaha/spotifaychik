using System;
using MusicService.Application.Users.Dtos;

namespace MusicService.API.Models
{
    public sealed class AuthResponse
    {
        /// <summary>access token для api</summary>
        public string AccessToken { get; set; } = string.Empty;
        /// <summary>refresh token для обновления</summary>
        public string RefreshToken { get; set; } = string.Empty;
        /// <summary>время окончания access token</summary>
        public DateTime AccessTokenExpiry { get; set; }
        /// <summary>время окончания refresh token</summary>
        public DateTime RefreshTokenExpiry { get; set; }
        /// <summary>данные пользователя</summary>
        public UserDto User { get; set; } = new();
    }
}
