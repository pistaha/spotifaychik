using MusicService.Application.Users.Dtos;

namespace MusicService.API.Models
{
    public sealed class RegisterUserResponse
    {
        /// <summary>пользователь</summary>
        public UserDto User { get; set; } = new();
        /// <summary>токен авторизации</summary>
        public AuthTokenResponse Token { get; set; } = new();
    }
}
