using FluentValidation;

namespace MusicService.Application.Tracks.Commands
{
    public class CreateTrackCommandValidator : AbstractValidator<CreateTrackCommand>
    {
        public CreateTrackCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Track title is required")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.DurationSeconds)
                .GreaterThan(0).WithMessage("Duration must be positive")
                .LessThanOrEqualTo(3600).WithMessage("Duration cannot exceed 1 hour");

            RuleFor(x => x.TrackNumber)
                .GreaterThan(0).WithMessage("Track number must be positive");

            RuleFor(x => x.AlbumId)
                .NotEmpty().WithMessage("Album ID is required");

            RuleFor(x => x.ArtistId)
                .NotEmpty().WithMessage("Artist ID is required");
        }
    }
}