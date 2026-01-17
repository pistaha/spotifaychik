using System;

namespace MusicService.Application.Authentication.Dtos
{
    public class RegisterDto
    {
        /// <summary>email пользователя</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>уникальный логин</summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>пароль для входа</summary>
        public string Password { get; set; } = string.Empty;
        /// <summary>повтор пароля</summary>
        public string ConfirmPassword { get; set; } = string.Empty;
        /// <summary>имя</summary>
        public string FirstName { get; set; } = string.Empty;
        /// <summary>фамилия</summary>
        public string LastName { get; set; } = string.Empty;
        /// <summary>дата рождения</summary>
        public DateTime? DateOfBirth { get; set; }
        /// <summary>телефон</summary>
        public string? PhoneNumber { get; set; }
    }

    public class LoginDto
    {
        /// <summary>email или логин</summary>
        public string EmailOrUsername { get; set; } = string.Empty;
        /// <summary>пароль</summary>
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenDto
    {
        /// <summary>истекший access token</summary>
        public string AccessToken { get; set; } = string.Empty;
        /// <summary>refresh token</summary>
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RevokeTokenDto
    {
        /// <summary>refresh token для отзыва</summary>
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        /// <summary>email для восстановления</summary>
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        /// <summary>токен для сброса</summary>
        public string Token { get; set; } = string.Empty;
        /// <summary>новый пароль</summary>
        public string NewPassword { get; set; } = string.Empty;
        /// <summary>повтор нового пароля</summary>
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        /// <summary>текущий пароль</summary>
        public string CurrentPassword { get; set; } = string.Empty;
        /// <summary>новый пароль</summary>
        public string NewPassword { get; set; } = string.Empty;
        /// <summary>повтор нового пароля</summary>
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
