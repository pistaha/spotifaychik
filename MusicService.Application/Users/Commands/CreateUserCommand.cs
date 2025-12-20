using MediatR;
using System;
using System.Collections.Generic;
using MusicService.Application.Users.Dtos;

namespace MusicService.Application.Users.Commands
{
    public record CreateUserCommand : IRequest<UserDto>
    {
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public DateTime? DateOfBirth { get; init; }
        public string Country { get; init; } = "Unknown";
        public List<string> FavoriteGenres { get; init; } = new();
    }
}