using FluentValidation;
using MusicService.Domain.Entities;

namespace MusicService.Application.Playlists.Commands
{
    public class CreatePlaylistCommandValidator : AbstractValidator<CreatePlaylistCommand>
    {
        public CreatePlaylistCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Playlist title is required")
                .MaximumLength(100).WithMessage("Title cannot exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Playlist type is required")
                .Must(BeValidPlaylistType).WithMessage("Invalid playlist type");

            RuleFor(x => x.CreatedBy)
                .NotEmpty().WithMessage("Creator ID is required");
        }

        private bool BeValidPlaylistType(string type)
        {
            return Enum.TryParse<PlaylistType>(type, out _);
        }
    }
}