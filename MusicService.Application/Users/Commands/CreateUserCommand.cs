using MediatR;
using System;
using System.Collections.Generic;
using MusicService.Application.Users.Dtos;

namespace MusicService.Application.Users.Commands
{
    public record CreateUserCommand : IRequest<UserDto>
    {
        /// <summary>уникальный логин</summary>
        public string Username { get; init; } = string.Empty;
        /// <summary>email пользователя</summary>
        public string Email { get; init; } = string.Empty;
        /// <summary>пароль для входа</summary>
        public string Password { get; init; } = string.Empty;
        /// <summary>имя</summary>
        public string FirstName { get; init; } = string.Empty;
        /// <summary>фамилия</summary>
        public string LastName { get; init; } = string.Empty;
        /// <summary>отображаемое имя</summary>
        public string DisplayName { get; init; } = string.Empty;
        /// <summary>дата рождения</summary>
        public DateTime? DateOfBirth { get; init; }
        /// <summary>страна</summary>
        public string Country { get; init; } = "Unknown";
        /// <summary>телефон</summary>
        public string? PhoneNumber { get; init; }
        /// <summary>любимые жанры</summary>
        public List<string> FavoriteGenres { get; init; } = new();
    }
}
