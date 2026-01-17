using System;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Authentication.Dtos;
using MusicService.Application.Common.Interfaces;

namespace MusicService.Application.Authentication.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public const string PasswordRuleMessage = "Password must be at least 8 characters long and include an uppercase letter, a lowercase letter, a digit, and a special character";
        private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new(@"^\+?[0-9]{10,15}$", RegexOptions.Compiled);
        private readonly IMusicServiceDbContext _dbContext;

        public RegisterDtoValidator(IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(200)
                .MustAsync(BeUniqueEmail).WithMessage("Email already exists");

            RuleFor(x => x.Username)
                .NotEmpty()
                .Length(3, 50)
                .Matches(UsernameRegex).WithMessage("Username can contain only letters, digits, and underscore")
                .MustAsync(BeUniqueUsername).WithMessage("Username already exists");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches(@"[A-Z]").WithMessage(PasswordRuleMessage)
                .Matches(@"[a-z]").WithMessage(PasswordRuleMessage)
                .Matches(@"\d").WithMessage(PasswordRuleMessage)
                .Matches(@"[^a-zA-Z0-9]").WithMessage(PasswordRuleMessage);

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.DateOfBirth)
                .Must(BeAdult).WithMessage("User must be at least 18 years old")
                .Must(NotInFuture).WithMessage("Date of birth cannot be in the future")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20)
                .Matches(PhoneRegex).When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }

        private Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return _dbContext.Users.AsNoTracking().AllAsync(u => u.Email != email, cancellationToken);
        }

        private Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            return _dbContext.Users.AsNoTracking().AllAsync(u => u.Username != username, cancellationToken);
        }

        private static bool BeAdult(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue)
            {
                return true;
            }

            var dob = dateOfBirth.Value.Date;
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dob.Year;
            if (today < dob.AddYears(age))
            {
                age--;
            }
            return age >= 18;
        }

        private static bool NotInFuture(DateTime? dateOfBirth)
        {
            return !dateOfBirth.HasValue || dateOfBirth.Value.Date <= DateTime.UtcNow.Date;
        }

    }

    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.EmailOrUsername).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
    {
        public RefreshTokenDtoValidator()
        {
            RuleFor(x => x.AccessToken).NotEmpty();
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    public class RevokeTokenDtoValidator : AbstractValidator<RevokeTokenDto>
    {
        public RevokeTokenDtoValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        }
    }

    public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordDtoValidator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .Matches(@"[A-Z]").WithMessage(RegisterDtoValidator.PasswordRuleMessage)
                .Matches(@"[a-z]").WithMessage(RegisterDtoValidator.PasswordRuleMessage)
                .Matches(@"\d").WithMessage(RegisterDtoValidator.PasswordRuleMessage)
                .Matches(@"[^a-zA-Z0-9]").WithMessage(RegisterDtoValidator.PasswordRuleMessage);
            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }

    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .Matches(@"[A-Z]").WithMessage(RegisterDtoValidator.PasswordRuleMessage)
                .Matches(@"[a-z]").WithMessage(RegisterDtoValidator.PasswordRuleMessage)
                .Matches(@"\d").WithMessage(RegisterDtoValidator.PasswordRuleMessage)
                .Matches(@"[^a-zA-Z0-9]").WithMessage(RegisterDtoValidator.PasswordRuleMessage);
            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }
}
