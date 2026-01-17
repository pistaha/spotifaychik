using System.Collections.Generic;
using MusicService.Application.Common.Dtos;

namespace MusicService.Application.Users.Dtos
{
    public class UserDto : BaseDto
    {
        /// <summary>уникальный логин</summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>email пользователя</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>имя</summary>
        public string FirstName { get; set; } = string.Empty;
        /// <summary>фамилия</summary>
        public string LastName { get; set; } = string.Empty;
        /// <summary>отображаемое имя</summary>
        public string DisplayName { get; set; } = string.Empty;
        /// <summary>ссылка на аватар</summary>
        public string? ProfileImage { get; set; }
        /// <summary>телефон</summary>
        public string? PhoneNumber { get; set; }
        /// <summary>страна</summary>
        public string Country { get; set; } = string.Empty;
        /// <summary>любимые жанры</summary>
        public List<string> FavoriteGenres { get; set; } = new();
        /// <summary>время прослушивания в минутах</summary>
        public int ListenTimeMinutes { get; set; }
        /// <summary>дата последнего входа</summary>
        public DateTime? LastLoginAt { get; set; }
        /// <summary>email подтвержден</summary>
        public bool IsEmailConfirmed { get; set; }
        /// <summary>аккаунт активен</summary>
        public bool IsActive { get; set; }
        /// <summary>количество плейлистов</summary>
        public int PlaylistCount { get; set; }
        /// <summary>количество подписок</summary>
        public int FollowingCount { get; set; }
        /// <summary>количество подписчиков</summary>
        public int FollowerCount { get; set; }
    }

    public class CreateUserDto
    {
        /// <summary>уникальный логин</summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>email пользователя</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>пароль для входа</summary>
        public string Password { get; set; } = string.Empty;
        /// <summary>имя</summary>
        public string FirstName { get; set; } = string.Empty;
        /// <summary>фамилия</summary>
        public string LastName { get; set; } = string.Empty;
        /// <summary>отображаемое имя</summary>
        public string DisplayName { get; set; } = string.Empty;
        /// <summary>дата рождения</summary>
        public DateTime? DateOfBirth { get; set; }
        /// <summary>страна</summary>
        public string Country { get; set; } = "Unknown";
        /// <summary>телефон</summary>
        public string? PhoneNumber { get; set; }
        /// <summary>любимые жанры</summary>
        public List<string> FavoriteGenres { get; set; } = new();
    }
}
