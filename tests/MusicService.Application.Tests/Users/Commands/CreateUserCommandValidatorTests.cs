using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Users.Commands;
using Xunit;

namespace Tests.MusicService.Application.Tests.Users.Commands;

public class CreateUserCommandValidatorTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenUsernameAndEmailAreUnique()
    {
        var validator = CreateValidator(false, false);
        var command = CreateValidCommand();

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenUsernameAlreadyExists()
    {
        var validator = CreateValidator(usernameExists: true, emailExists: false);
        var command = CreateValidCommand();

        var result = await validator.ValidateAsync(command, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreateUserCommand.Username) &&
            e.ErrorMessage.Contains("exists", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenEmailAlreadyExists()
    {
        var validator = CreateValidator(usernameExists: false, emailExists: true);
        var command = CreateValidCommand();

        var result = await validator.ValidateAsync(command, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreateUserCommand.Email) &&
            e.ErrorMessage.Contains("exists", StringComparison.OrdinalIgnoreCase));
    }

    private CreateUserCommandValidator CreateValidator(bool usernameExists, bool emailExists)
    {
        _userRepository.Reset();
        _userRepository
            .Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usernameExists);
        _userRepository
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailExists);

        return new CreateUserCommandValidator(_userRepository.Object);
    }

    private CreateUserCommand CreateValidCommand()
    {
        return new CreateUserCommand
        {
            Username = "test_user",
            Email = "test@example.com",
            Password = "P@ssw0rd!",
            DisplayName = "Test User",
            Country = "UA"
        };
    }
}
