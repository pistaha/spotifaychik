using FluentValidation;
namespace MusicService.Application.Users.Commands
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private const string PasswordRuleMessage = "Password must be at least 8 characters long and include an uppercase letter, a lowercase letter, and a digit";
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .Matches(@"^[a-zA-Z0-9._-]{3,50}$").WithMessage("Username can contain only letters, digits, dot, underscore, and dash");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage(PasswordRuleMessage)
                .Matches(@"[A-Z]").WithMessage(PasswordRuleMessage)
                .Matches(@"[a-z]").WithMessage(PasswordRuleMessage)
                .Matches(@"\d").WithMessage(PasswordRuleMessage);

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .MaximumLength(150).WithMessage("Display name cannot exceed 150 characters");
        }
    }
}
