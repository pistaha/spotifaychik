using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Users.Commands;
using MusicService.Application.Albums.Queries;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Security;
using Xunit;

namespace Tests.EFCoreTests
{
    public class ApplicationHandlersTests
    {
        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            return config.CreateMapper();
        }

        [Fact]
        public async Task CreateUserCommandHandler_PersistsUser()
        {
            var mapper = CreateMapper();
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var hasher = new BcryptPasswordHasher();
            var handler = new CreateUserCommandHandler(dbContext, hasher, mapper, new Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateUserCommandHandler>());

            var command = new CreateUserCommand
            {
                Username = "demo_user",
                Email = "demo_user@music.local",
                Password = "password",
                DisplayName = "Demo User",
                Country = "US"
            };

            var initialCount = await dbContext.Users.CountAsync();
            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Username.Should().Be("demo_user");
            (await dbContext.Users.CountAsync()).Should().Be(initialCount + 1);
        }

        [Fact]
        public async Task GetAlbumByIdQueryHandler_ReturnsAlbumWithTracks()
        {
            var mapper = CreateMapper();
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());

            var artistId = Guid.NewGuid();
            var albumId = Guid.NewGuid();
            var trackId = Guid.NewGuid();

            dbContext.Artists.Add(new Artist { Id = artistId, Name = "Artist A" });
            dbContext.Albums.Add(new Album { Id = albumId, Title = "Album A", ArtistId = artistId, ReleaseDate = DateTime.UtcNow, Type = AlbumType.Album });
            dbContext.Tracks.Add(new Track { Id = trackId, Title = "Track A", AlbumId = albumId, ArtistId = artistId, DurationSeconds = 180, TrackNumber = 1 });
            await dbContext.SaveChangesAsync();

            var handler = new GetAlbumByIdQueryHandler(dbContext, mapper);
            var result = await handler.Handle(new GetAlbumByIdQuery { AlbumId = albumId }, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Title.Should().Be("Album A");
            result.TrackCount.Should().Be(1);
        }
    }
}
