using FluentAssertions;
using MusicService.Application.Users.Commands;
using Xunit;

namespace Tests.MusicService.Application.Tests.Users.Commands;

public class CreateUserCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_ForValidCommand()
    {
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand
        {
            Username = "test_user",
            Email = "test@example.com",
            Password = "P@ssw0rd!",
            DisplayName = "Test User",
            Country = "UA"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForMissingFields()
    {
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand
        {
            Username = "",
            Email = "bad",
            Password = "",
            DisplayName = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Username));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Email));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Password));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.DisplayName));
    }
}
