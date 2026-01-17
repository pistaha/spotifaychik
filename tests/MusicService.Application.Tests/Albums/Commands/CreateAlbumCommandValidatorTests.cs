using System;
using FluentAssertions;
using MusicService.Application.Albums.Commands;
using Xunit;

namespace Tests.MusicService.Application.Tests.Albums.Commands;

public class CreateAlbumCommandValidatorTests
{
    private readonly CreateAlbumCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_ForValidCommand()
    {
        var command = new CreateAlbumCommand
        {
            Title = "New Horizons",
            Description = "A stellar release",
            ReleaseDate = DateTime.UtcNow.AddDays(-1),
            Type = "Album",
            ArtistId = Guid.NewGuid(),
            CreatedById = Guid.NewGuid()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleMissingOrTooLong()
    {
        var command = new CreateAlbumCommand
        {
            Title = string.Empty,
            Description = new string('a', 1001),
            ReleaseDate = DateTime.UtcNow.AddDays(-1),
            Type = "Album",
            ArtistId = Guid.NewGuid(),
            CreatedById = Guid.NewGuid()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAlbumCommand.Title));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAlbumCommand.Description));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTypeIsInvalid()
    {
        var command = new CreateAlbumCommand
        {
            Title = "Experimental Record",
            ReleaseDate = DateTime.UtcNow.AddDays(-1),
            Type = "Gibberish",
            ArtistId = Guid.NewGuid(),
            CreatedById = Guid.NewGuid()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateAlbumCommand.Type) &&
            e.ErrorMessage.Contains("Invalid album type", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ShouldFail_WhenReleaseDateInFuture()
    {
        var command = new CreateAlbumCommand
        {
            Title = "Future Drop",
            ReleaseDate = DateTime.UtcNow.AddDays(10),
            Type = "Album",
            ArtistId = Guid.NewGuid(),
            CreatedById = Guid.NewGuid()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAlbumCommand.ReleaseDate));
    }
}
