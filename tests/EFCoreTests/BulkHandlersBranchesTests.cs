using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Users.Commands;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Persistence;
using Xunit;

namespace Tests.EFCoreTests
{
    public class BulkHandlersBranchesTests
    {
        private static MusicServiceDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MusicServiceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new MusicServiceDbContext(options);
        }

        [Fact]
        public async Task BulkCreateAlbums_ShouldHandleInvalidTypeAndMissingArtist()
        {
            var dbContext = CreateInMemoryDbContext();
            var artist = new Artist
            {
                Id = Guid.NewGuid(),
                Name = "Artist",
                Country = "US",
                Genres = new List<string> { "Rock" }
            };
            dbContext.Artists.Add(artist);
            await dbContext.SaveChangesAsync();

            var handler = new BulkCreateAlbumsCommandHandler(
                dbContext,
                TestMapperFactory.Create(),
                NullLogger<BulkCreateAlbumsCommandHandler>.Instance);

            var result = await handler.Handle(new BulkCreateAlbumsCommand
            {
                Commands = new List<CreateAlbumCommand>
                {
                    new()
                    {
                        Title = "Bad Type",
                        ArtistId = artist.Id,
                        ReleaseDate = DateTime.UtcNow,
                        Type = "NotAType",
                        Genres = new List<string> { "Rock" }
                    },
                    new()
                    {
                        Title = "Missing Artist",
                        ArtistId = Guid.NewGuid(),
                        ReleaseDate = DateTime.UtcNow,
                        Type = "Album",
                        Genres = new List<string> { "Rock" }
                    },
                    new()
                    {
                        Title = "Ok Album",
                        ArtistId = artist.Id,
                        ReleaseDate = DateTime.UtcNow,
                        Type = "Album",
                        Genres = new List<string> { "Rock" }
                    }
                }
            }, CancellationToken.None);

            result.Items.Should().HaveCount(3);
            result.Items.Should().Contain(i => !i.Success && i.Error == "Invalid album type");
            result.Items.Should().Contain(i => !i.Success && i.Error == "Artist not found");
            result.Items.Should().Contain(i => i.Success);
        }

        [Fact]
        public async Task BulkCreateUsers_ShouldHandleDuplicateEmailAndUsername()
        {
            var dbContext = CreateInMemoryDbContext();
            var handler = new BulkCreateUsersCommandHandler(
                dbContext,
                new global::MusicService.Infrastructure.Security.BcryptPasswordHasher(),
                TestMapperFactory.Create(),
                NullLogger<BulkCreateUsersCommandHandler>.Instance);

            var result = await handler.Handle(new BulkCreateUsersCommand
            {
                Commands = new List<CreateUserCommand>
                {
                    new()
                    {
                        Username = "dup",
                        Email = "dup@music.local",
                        Password = "password",
                        DisplayName = "User A"
                    },
                    new()
                    {
                        Username = "dup",
                        Email = "unique@music.local",
                        Password = "password",
                        DisplayName = "User B"
                    },
                    new()
                    {
                        Username = "unique",
                        Email = "dup@music.local",
                        Password = "password",
                        DisplayName = "User C"
                    }
                }
            }, CancellationToken.None);

            result.Items.Should().HaveCount(3);
            result.Items.Should().Contain(i => !i.Success && i.Error == "Username already in use");
            result.Items.Should().Contain(i => !i.Success && i.Error == "Email already in use");
            result.SuccessfulCount.Should().Be(1);
        }

        [Fact]
        public async Task BulkCreateAlbums_ShouldMarkDuplicateAlbum_WhenUniqueViolation()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var artist = new Artist
            {
                Id = Guid.NewGuid(),
                Name = "Artist",
                Country = "US"
            };
            dbContext.Artists.Add(artist);
            dbContext.Albums.Add(new Album
            {
                Id = Guid.NewGuid(),
                Title = "Dup Album",
                ArtistId = artist.Id,
                ReleaseDate = DateTime.UtcNow,
                Type = AlbumType.Album
            });
            await dbContext.SaveChangesAsync();

            var handler = new BulkCreateAlbumsCommandHandler(
                dbContext,
                TestMapperFactory.Create(),
                NullLogger<BulkCreateAlbumsCommandHandler>.Instance);

            var result = await handler.Handle(new BulkCreateAlbumsCommand
            {
                Commands = new List<CreateAlbumCommand>
                {
                    new()
                    {
                        Title = "Dup Album",
                        ArtistId = artist.Id,
                        ReleaseDate = DateTime.UtcNow,
                        Type = "Album",
                        Genres = new List<string>()
                    }
                }
            }, CancellationToken.None);

            result.Items.Should().ContainSingle(i => !i.Success && i.Error == "Album already exists");
        }

        [Fact]
        public async Task BulkCreateUsers_ShouldMarkDuplicateUser_WhenUniqueViolation()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = "dupuser",
                Email = "dup@music.local",
                PasswordHash = "hash",
                DisplayName = "Dup User",
                Country = "US"
            });
            await dbContext.SaveChangesAsync();

            var handler = new BulkCreateUsersCommandHandler(
                dbContext,
                new global::MusicService.Infrastructure.Security.BcryptPasswordHasher(),
                TestMapperFactory.Create(),
                NullLogger<BulkCreateUsersCommandHandler>.Instance);

            var result = await handler.Handle(new BulkCreateUsersCommand
            {
                Commands = new List<CreateUserCommand>
                {
                    new()
                    {
                        Username = "dupuser",
                        Email = "dup@music.local",
                        Password = "password",
                        DisplayName = "Dup User"
                    }
                }
            }, CancellationToken.None);

            result.Items.Should().ContainSingle(i => !i.Success && i.Error == "Email or username already in use");
        }

        [Fact]
        public async Task BulkDeleteAlbums_ShouldHandleExistingAndMissing_InMemory()
        {
            var dbContext = CreateInMemoryDbContext();
            var albumId = Guid.NewGuid();
            dbContext.Albums.Add(new Album
            {
                Id = albumId,
                Title = "Album",
                ArtistId = Guid.NewGuid(),
                ReleaseDate = DateTime.UtcNow,
                Type = AlbumType.Album
            });
            await dbContext.SaveChangesAsync();

            var handler = new BulkDeleteAlbumsCommandHandler(
                dbContext,
                NullLogger<BulkDeleteAlbumsCommandHandler>.Instance);

            var result = await handler.Handle(new BulkDeleteAlbumsCommand
            {
                AlbumIds = new List<Guid> { albumId, Guid.NewGuid() }
            }, CancellationToken.None);

            result.SuccessfulCount.Should().Be(1);
            result.FailedCount.Should().Be(1);
        }
    }
}
