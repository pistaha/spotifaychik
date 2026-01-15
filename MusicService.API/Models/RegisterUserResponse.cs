using MusicService.Application.Users.Dtos;

namespace MusicService.API.Models
{
    public sealed class RegisterUserResponse
    {
        public UserDto User { get; set; } = new();
        public AuthTokenResponse Token { get; set; } = new();
    }
}
