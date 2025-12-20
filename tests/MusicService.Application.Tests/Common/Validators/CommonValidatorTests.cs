using FluentAssertions;
using FluentValidation;
using MediatR;
using MusicService.Application.Common.Behaviors;
using Xunit;

namespace Tests.MusicService.Application.Tests.Common.Validators;

public class CommonValidatorTests
{
    private record TestCommand(string Name) : IRequest<string>;

    private class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator() => RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
    }

    [Fact]
    public async Task ValidationBehavior_ShouldThrowValidationException_WhenValidationFails()
    {
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand(string.Empty);

        var act = async () => await behavior.Handle(command, () => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ValidationBehavior_ShouldCallNext_WhenValidationPasses()
    {
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("valid");
        var called = false;

        var result = await behavior.Handle(command, () =>
        {
            called = true;
            return Task.FromResult("ok");
        }, CancellationToken.None);

        called.Should().BeTrue();
        result.Should().Be("ok");
    }
}
