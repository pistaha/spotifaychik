using FluentValidation;

namespace MusicService.Application.Artists.Commands
{
    public class CreateArtistCommandValidator : AbstractValidator<CreateArtistCommand>
    {
        public CreateArtistCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Artist name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Biography)
                .MaximumLength(2000).WithMessage("Biography cannot exceed 2000 characters");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required");
        }
    }
}