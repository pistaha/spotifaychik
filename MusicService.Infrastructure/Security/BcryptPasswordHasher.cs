using System;
using MusicService.Application.Common.Interfaces;

namespace MusicService.Infrastructure.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
