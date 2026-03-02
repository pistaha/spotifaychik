using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Playlists.Commands;
using MusicService.Application.Tracks.Commands;
using MusicService.Application.Users.Commands;
using Xunit;

namespace Tests.EFCoreTests
{
    public class ValidatorCoverageTests
    {
        [Fact]
        public async Task CreateUserCommandValidator_ShouldRejectInvalidInput()
        {
            var validator = new CreateUserCommandValidator();
            var result = await validator.ValidateAsync(new CreateUserCommand
            {
                Username = string.Empty,
                Email = "invalid",
                Password = "123",
                DisplayName = string.Empty
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void CreateAlbumCommandValidator_ShouldValidate()
        {
            var validator = new CreateAlbumCommandValidator();
            ValidationResult result = validator.Validate(new CreateAlbumCommand
            {
                Title = "Album",
                Type = "Album",
                ReleaseDate = DateTime.UtcNow.AddDays(-1),
                ArtistId = Guid.NewGuid(),
                CreatedById = Guid.NewGuid(),
                Genres = new List<string> { "Rock" }
            });

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void CreateArtistCommandValidator_ShouldValidate()
        {
            var validator = new CreateArtistCommandValidator();
            var result = validator.Validate(new CreateArtistCommand
            {
                Name = "Artist",
                Country = "US",
                CreatedById = Guid.NewGuid()
            });

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void CreateTrackCommandValidator_ShouldValidate()
        {
            var validator = new CreateTrackCommandValidator();
            var result = validator.Validate(new CreateTrackCommand
            {
                Title = "Track",
                DurationSeconds = 120,
                TrackNumber = 1,
                AlbumId = Guid.NewGuid(),
                ArtistId = Guid.NewGuid(),
                CreatedById = Guid.NewGuid()
            });

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void CreatePlaylistCommandValidator_ShouldValidate()
        {
            var validator = new CreatePlaylistCommandValidator();
            var result = validator.Validate(new CreatePlaylistCommand
            {
                Title = "Playlist",
                Type = "UserCreated",
                CreatedBy = Guid.NewGuid(),
                IsPublic = true
            });

            result.IsValid.Should().BeTrue();
        }
    }
}
