using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Playlists.Commands;
using MusicService.Application.Tracks.Commands;
using MusicService.Application.Users.Commands;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Security;
using Xunit;

namespace Tests.EFCoreTests
{
    public class CreateHandlersBranchesTests
    {
        [Fact]
        public async Task CreateAlbum_ShouldThrow_WhenArtistMissing()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var creatorId = Guid.NewGuid();
            dbContext.Users.Add(new User
            {
                Id = creatorId,
                Username = "creator",
                Email = "creator@music.local",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                DisplayName = "Creator",
                Country = "US"
            });
            await dbContext.SaveChangesAsync();
            var handler = new CreateAlbumCommandHandler(dbContext, TestMapperFactory.Create(), NullLogger<CreateAlbumCommandHandler>.Instance);

            Func<Task> act = () => handler.Handle(new CreateAlbumCommand
            {
                Title = "Album",
                ArtistId = Guid.NewGuid(),
                CreatedById = creatorId,
                ReleaseDate = DateTime.UtcNow,
                Type = "Album",
                Genres = new()
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateAlbum_ShouldThrow_WhenDuplicateTitlePerArtist()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var artist = new Artist { Id = Guid.NewGuid(), Name = "Artist", Country = "US" };
            var creatorId = Guid.NewGuid();
            dbContext.Artists.Add(artist);
            dbContext.Users.Add(new User
            {
                Id = creatorId,
                Username = "creator",
                Email = "creator@music.local",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                DisplayName = "Creator",
                Country = "US"
            });
            dbContext.Albums.Add(new Album
            {
                Id = Guid.NewGuid(),
                Title = "Dup Album",
                ArtistId = artist.Id,
                ReleaseDate = DateTime.UtcNow,
                Type = AlbumType.Album
            });
            await dbContext.SaveChangesAsync();

            var handler = new CreateAlbumCommandHandler(dbContext, TestMapperFactory.Create(), NullLogger<CreateAlbumCommandHandler>.Instance);
            Func<Task> act = () => handler.Handle(new CreateAlbumCommand
            {
                Title = "Dup Album",
                ArtistId = artist.Id,
                CreatedById = creatorId,
                ReleaseDate = DateTime.UtcNow,
                Type = "Album",
                Genres = new()
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateTrack_ShouldThrow_WhenAlbumMissing()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var artist = new Artist { Id = Guid.NewGuid(), Name = "Artist", Country = "US" };
            var creatorId = Guid.NewGuid();
            dbContext.Artists.Add(artist);
            dbContext.Users.Add(new User
            {
                Id = creatorId,
                Username = "creator",
                Email = "creator@music.local",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                DisplayName = "Creator",
                Country = "US"
            });
            await dbContext.SaveChangesAsync();

            var handler = new CreateTrackCommandHandler(dbContext, TestMapperFactory.Create(), NullLogger<CreateTrackCommandHandler>.Instance);
            Func<Task> act = () => handler.Handle(new CreateTrackCommand
            {
                Title = "Track",
                DurationSeconds = 180,
                TrackNumber = 1,
                AlbumId = Guid.NewGuid(),
                ArtistId = artist.Id,
                CreatedById = creatorId,
                IsExplicit = false
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateTrack_ShouldThrow_WhenDuplicateTrackNumber()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var artist = new Artist { Id = Guid.NewGuid(), Name = "Artist", Country = "US" };
            var album = new Album
            {
                Id = Guid.NewGuid(),
                Title = "Album",
                ArtistId = artist.Id,
                ReleaseDate = DateTime.UtcNow,
                Type = AlbumType.Album
            };
            var creatorId = Guid.NewGuid();
            dbContext.Artists.Add(artist);
            dbContext.Albums.Add(album);
            dbContext.Users.Add(new User
            {
                Id = creatorId,
                Username = "creator",
                Email = "creator@music.local",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                DisplayName = "Creator",
                Country = "US"
            });
            dbContext.Tracks.Add(new Track
            {
                Id = Guid.NewGuid(),
                Title = "Track A",
                DurationSeconds = 180,
                TrackNumber = 1,
                AlbumId = album.Id,
                ArtistId = artist.Id
            });
            await dbContext.SaveChangesAsync();

            var handler = new CreateTrackCommandHandler(dbContext, TestMapperFactory.Create(), NullLogger<CreateTrackCommandHandler>.Instance);
            Func<Task> act = () => handler.Handle(new CreateTrackCommand
            {
                Title = "Track B",
                DurationSeconds = 200,
                TrackNumber = 1,
                AlbumId = album.Id,
                ArtistId = artist.Id,
                CreatedById = creatorId,
                IsExplicit = false
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreatePlaylist_ShouldThrow_WhenUserMissing()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var handler = new CreatePlaylistCommandHandler(dbContext, TestMapperFactory.Create(), NullLogger<CreatePlaylistCommandHandler>.Instance);

            Func<Task> act = () => handler.Handle(new CreatePlaylistCommand
            {
                Title = "Playlist",
                CreatedBy = Guid.NewGuid(),
                IsPublic = true,
                IsCollaborative = false,
                Type = "UserCreated"
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreatePlaylist_ShouldThrow_WhenDuplicateTitleForUser()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "user",
                Email = "user@music.local",
                PasswordHash = "hash",
                DisplayName = "User",
                Country = "US"
            };
            dbContext.Users.Add(user);
            dbContext.Playlists.Add(new Playlist
            {
                Id = Guid.NewGuid(),
                Title = "Dup Playlist",
                CreatedById = user.Id,
                Type = PlaylistType.UserCreated
            });
            await dbContext.SaveChangesAsync();

            var handler = new CreatePlaylistCommandHandler(dbContext, TestMapperFactory.Create(), NullLogger<CreatePlaylistCommandHandler>.Instance);
            Func<Task> act = () => handler.Handle(new CreatePlaylistCommand
            {
                Title = "Dup Playlist",
                CreatedBy = user.Id,
                IsPublic = true,
                IsCollaborative = false,
                Type = "UserCreated"
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateArtist_ShouldThrow_WhenDuplicateName()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var creatorId = Guid.NewGuid();
            dbContext.Users.Add(new User
            {
                Id = creatorId,
                Username = "creator",
                Email = "creator@music.local",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                DisplayName = "Creator",
                Country = "US"
            });
            dbContext.Artists.Add(new Artist { Id = Guid.NewGuid(), Name = "Dup", Country = "US" });
            await dbContext.SaveChangesAsync();

            var handler = new CreateArtistCommandHandler(dbContext, TestMapperFactory.Create(), NullLogger<CreateArtistCommandHandler>.Instance);
            Func<Task> act = () => handler.Handle(new CreateArtistCommand
            {
                Name = "Dup",
                Genres = new(),
                Country = "US",
                CreatedById = creatorId
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateUser_ShouldThrow_WhenEmailExists()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var hasher = new BcryptPasswordHasher();
            dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = "user1",
                Email = "dup@music.local",
                PasswordHash = "hash",
                DisplayName = "User One",
                Country = "US"
            });
            await dbContext.SaveChangesAsync();

            var handler = new CreateUserCommandHandler(dbContext, hasher, TestMapperFactory.Create(), NullLogger<CreateUserCommandHandler>.Instance);
            Func<Task> act = () => handler.Handle(new CreateUserCommand
            {
                Username = "user2",
                Email = "dup@music.local",
                Password = "password",
                DisplayName = "User Two",
                Country = "US"
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateUser_ShouldThrow_WhenUsernameExists()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var hasher = new BcryptPasswordHasher();
            dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = "dupuser",
                Email = "user@music.local",
                PasswordHash = "hash",
                DisplayName = "User One",
                Country = "US"
            });
            await dbContext.SaveChangesAsync();

            var handler = new CreateUserCommandHandler(dbContext, hasher, TestMapperFactory.Create(), NullLogger<CreateUserCommandHandler>.Instance);
            Func<Task> act = () => handler.Handle(new CreateUserCommand
            {
                Username = "dupuser",
                Email = "unique@music.local",
                Password = "password",
                DisplayName = "User Two",
                Country = "US"
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
