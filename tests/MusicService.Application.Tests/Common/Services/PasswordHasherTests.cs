using FluentAssertions;
using MusicService.Infrastructure.Security;
using Xunit;

namespace Tests.MusicService.Application.Tests.Common.Services;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldReturnHashedValue()
    {
        var hasher = new BcryptPasswordHasher();

        var hashed = hasher.HashPassword("secret");

        hashed.Should().NotBeNullOrWhiteSpace();
        hashed.Should().NotBe("secret");
        BCrypt.Net.BCrypt.Verify("secret", hashed).Should().BeTrue();
    }

    [Fact]
    public void HashPassword_ShouldThrow_WhenPasswordEmpty()
    {
        var hasher = new BcryptPasswordHasher();

        Action act = () => hasher.HashPassword("");

        act.Should().Throw<ArgumentException>();
    }
}
