using System;
using FluentAssertions;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.EFCoreTests
{
    public class DomainEntitiesTests
    {
        [Fact]
        public void DomainEntities_ShouldComputeDerivedValues()
        {
            var artist = new Artist
            {
                Name = "Artist",
                Genres = { "Rock", "Pop" },
                CareerStartDate = DateTime.UtcNow.AddYears(-4)
            };

            var album = new Album
            {
                Title = "Album",
                ReleaseDate = DateTime.UtcNow.AddDays(-5),
                Type = AlbumType.Single
            };

            var track = new Track
            {
                Title = "Track",
                DurationSeconds = 125
            };

            var user = new User
            {
                Username = "user",
                DateOfBirth = DateTime.UtcNow.AddYears(-20)
            };

            var playlist = new Playlist
            {
                Title = "Playlist",
                CreatedById = Guid.NewGuid(),
                IsCollaborative = false
            };

            var history = new ListenHistory
            {
                ListenedAt = DateTime.UtcNow
            };

            artist.HasGenre("rock").Should().BeTrue();
            artist.YearsActive.Should().BeGreaterThan(0);

            album.IsRecentRelease().Should().BeTrue();
            album.IsSingle.Should().BeTrue();

            track.DurationFormatted.Should().Be("2:05");
            var previousPlayCount = track.PlayCount;
            track.IncrementPlayCount();
            track.PlayCount.Should().Be(previousPlayCount + 1);

            user.IsAdult().Should().BeTrue();
            var previousListenTime = user.ListenTimeMinutes;
            user.AddListenTime(15);
            user.ListenTimeMinutes.Should().Be(previousListenTime + 15);

            playlist.CanBeEditedBy(new User { Id = playlist.CreatedById }).Should().BeTrue();
            playlist.TrackCount.Should().Be(0);

            history.WasListenedToday.Should().BeTrue();
        }
    }
}
