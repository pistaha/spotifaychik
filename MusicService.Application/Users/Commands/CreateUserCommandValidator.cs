using FluentValidation;
namespace MusicService.Application.Users.Commands
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private const string PasswordRuleMessage = "Password must be at least 8 characters long and include an uppercase letter, a lowercase letter, a digit, and a special character";
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .Matches(@"^[a-zA-Z0-9_]{3,50}$").WithMessage("Username can contain only letters, digits, and underscore");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(200).WithMessage("Email cannot exceed 200 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage(PasswordRuleMessage)
                .Matches(@"[A-Z]").WithMessage(PasswordRuleMessage)
                .Matches(@"[a-z]").WithMessage(PasswordRuleMessage)
                .Matches(@"\d").WithMessage(PasswordRuleMessage)
                .Matches(@"[^a-zA-Z0-9]").WithMessage(PasswordRuleMessage);

            RuleFor(x => x.DisplayName)
                .MaximumLength(150).WithMessage("Display name cannot exceed 150 characters");
        }
    }
}
