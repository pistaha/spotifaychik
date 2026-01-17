using FluentValidation;
using MusicService.Domain.Entities;
using System;

namespace MusicService.Application.Albums.Commands
{
    public class CreateAlbumCommandValidator : AbstractValidator<CreateAlbumCommand>
    {
        public CreateAlbumCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Album title is required")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Album type is required")
                .Must(BeValidAlbumType).WithMessage("Invalid album type");

            RuleFor(x => x.ReleaseDate)
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Release date cannot be in the future");

            RuleFor(x => x.ArtistId)
                .NotEmpty().WithMessage("Artist ID is required");

            RuleFor(x => x.CreatedById)
                .NotEmpty().WithMessage("CreatedBy ID is required");
        }

        private bool BeValidAlbumType(string type)
        {
            return Enum.TryParse<AlbumType>(type, out _);
        }
    }
}
