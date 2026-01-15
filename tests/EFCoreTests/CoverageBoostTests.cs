using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using MusicService.Application.Common.Behaviors;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Search.Mapping;
using MusicService.Application.Users.Dtos;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Search.Dtos;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Configuration;
using Xunit;

namespace Tests.EFCoreTests
{
    public class CoverageBoostTests
    {
        [Fact]
        public void MappingProfiles_ShouldMapEntities()
        {
            var mapper = CreateMapper();
            var user = new User
            {
                Username = "user",
                Email = "user@music.local",
                DisplayName = "User",
                CreatedPlaylists = new List<Playlist> { new() },
                FollowedArtists = new List<Artist> { new() },
                FollowedPlaylists = new List<Playlist> { new() },
                Friends = new List<User> { new() }
            };

            var artist = new Artist
            {
                Name = "Artist",
                Albums = new List<Album> { new() },
                Tracks = new List<Track> { new(), new() },
                Followers = new List<User> { new() }
            };

            var album = new Album
            {
                Title = "Album",
                Type = AlbumType.Album,
                ReleaseDate = DateTime.UtcNow.AddDays(-1),
                Artist = artist,
                Tracks = new List<Track> { new() }
            };

            var track = new Track
            {
                Title = "Track",
                DurationSeconds = 120,
                Album = album,
                Artist = artist
            };

            var playlist = new Playlist
            {
                Title = "Playlist",
                Type = PlaylistType.UserCreated,
                CreatedBy = user,
                PlaylistTracks = new List<PlaylistTrack> { new() }
            };

            mapper.Map<UserDto>(user).PlaylistCount.Should().Be(1);
            mapper.Map<ArtistDto>(artist).TrackCount.Should().Be(2);
            mapper.Map<AlbumDto>(album).ArtistName.Should().Be("Artist");
            mapper.Map<TrackDto>(track).AlbumTitle.Should().Be("Album");
            mapper.Map<PlaylistDto>(playlist).CreatedByName.Should().Be("user");
        }

        [Fact]
        public void SearchMappingProfile_ShouldMapSearchDtos()
        {
            var mapper = CreateMapper();
            var artist = new Artist { Name = "Artist", ProfileImage = "p.png" };
            var album = new Album { Title = "Album", Artist = artist, ReleaseDate = new DateTime(2020, 1, 1) };
            var track = new Track { Title = "Track", Artist = artist, Album = album, DurationSeconds = 123 };
            var playlist = new Playlist { Title = "Playlist", CreatedBy = new User { Username = "user" }, PlaylistTracks = new List<PlaylistTrack>() };
            var user = new User { Username = "user" };

            mapper.Map<ArtistSearchResultDto>(artist).Name.Should().Be("Artist");
            mapper.Map<AlbumSearchResultDto>(album).ReleaseYear.Should().Be(2020);
            mapper.Map<TrackSearchResultDto>(track).DurationSeconds.Should().Be(123);
            mapper.Map<PlaylistSearchResultDto>(playlist).CreatorName.Should().Be("user");
            mapper.Map<UserSearchResultDto>(user).Username.Should().Be("user");

            mapper.Map<GlobalArtistDto>(artist).ImageUrl.Should().Be("p.png");
            mapper.Map<GlobalAlbumDto>(album).ArtistName.Should().Be("Artist");
            mapper.Map<GlobalTrackDto>(track).ArtistName.Should().Be("Artist");
            mapper.Map<GlobalPlaylistDto>(playlist).CreatorName.Should().Be("user");
        }

        [Fact]
        public void AddInfrastructure_ShouldThrowWhenConnectionStringMissing()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            Action act = () => services.AddInfrastructure(config);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task ValidationBehavior_ShouldPassWhenNoValidators()
        {
            var behavior = new ValidationBehavior<DummyRequest, bool>(Array.Empty<IValidator<DummyRequest>>());
            var request = new DummyRequest { Name = "ok" };

            var result = await behavior.Handle(request, () => Task.FromResult(true), CancellationToken.None);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DbContext_SaveChangesAsync_ShouldUpdateTimestampOnModify()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var user = new User
            {
                Username = "user",
                Email = "user@music.local",
                PasswordHash = "hash",
                DisplayName = "User",
                Country = "US"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            var firstUpdatedAt = user.UpdatedAt;

            user.DisplayName = "User Updated";
            await dbContext.SaveChangesAsync();

            user.UpdatedAt.Should().BeAfter(firstUpdatedAt);
        }

        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
                cfg.AddProfile<SearchMappingProfile>();
            });
            return config.CreateMapper();
        }

        private sealed class DummyRequest : MediatR.IRequest<bool>
        {
            public string Name { get; init; } = string.Empty;
        }
    }
}
