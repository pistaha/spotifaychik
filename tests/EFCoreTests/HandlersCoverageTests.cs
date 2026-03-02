using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Albums.Queries;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Artists.Queries;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Playlists.Commands;
using MusicService.Application.Playlists.Queries;
using MusicService.Application.Tracks.Commands;
using MusicService.Application.Tracks.Queries;
using MusicService.Application.Users.Commands;
using MusicService.Application.Users.Queries;
using MusicService.Application.AI.Queries;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Security;
using Xunit;

namespace Tests.EFCoreTests
{
    public class HandlersCoverageTests
    {
        [Fact]
        public async Task Handlers_ShouldHandleCommonPaths()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var seed = await TestDataSeeder.SeedAsync(dbContext);
            var mapper = TestMapperFactory.Create();
            var hasher = new BcryptPasswordHasher();

            var createUserHandler = new CreateUserCommandHandler(
                dbContext,
                hasher,
                mapper,
                NullLogger<CreateUserCommandHandler>.Instance);
            var createdUser = await createUserHandler.Handle(new CreateUserCommand
            {
                Username = "new_user",
                Email = "new_user@music.local",
                Password = "password",
                DisplayName = "New User",
                Country = "US"
            }, CancellationToken.None);
            createdUser.Username.Should().Be("new_user");

            var createArtistHandler = new CreateArtistCommandHandler(
                dbContext,
                mapper,
                NullLogger<CreateArtistCommandHandler>.Instance);
            var createdArtist = await createArtistHandler.Handle(new CreateArtistCommand
            {
                Name = "Artist B",
                Genres = new List<string> { "Jazz" },
                Country = "US",
                CreatedById = createdUser.Id
            }, CancellationToken.None);
            createdArtist.Name.Should().Be("Artist B");

            var createAlbumHandler = new CreateAlbumCommandHandler(
                dbContext,
                mapper,
                NullLogger<CreateAlbumCommandHandler>.Instance);
            var createdAlbum = await createAlbumHandler.Handle(new CreateAlbumCommand
            {
                Title = "Album B",
                ArtistId = createdArtist.Id,
                CreatedById = createdUser.Id,
                ReleaseDate = DateTime.UtcNow.AddDays(-2),
                Type = "Album",
                Genres = new List<string> { "Jazz" }
            }, CancellationToken.None);
            createdAlbum.Title.Should().Be("Album B");

            var createTrackHandler = new CreateTrackCommandHandler(
                dbContext,
                mapper,
                NullLogger<CreateTrackCommandHandler>.Instance);
            var createdTrack = await createTrackHandler.Handle(new CreateTrackCommand
            {
                Title = "Track B",
                DurationSeconds = 200,
                TrackNumber = 1,
                AlbumId = createdAlbum.Id,
                ArtistId = createdArtist.Id,
                CreatedById = createdUser.Id,
                IsExplicit = false
            }, CancellationToken.None);
            createdTrack.Title.Should().Be("Track B");

            var createPlaylistHandler = new CreatePlaylistCommandHandler(
                dbContext,
                mapper,
                NullLogger<CreatePlaylistCommandHandler>.Instance);
            var createdPlaylist = await createPlaylistHandler.Handle(new CreatePlaylistCommand
            {
                Title = "Playlist B",
                CreatedBy = createdUser.Id,
                IsPublic = true,
                IsCollaborative = false,
                Type = "UserCreated"
            }, CancellationToken.None);
            createdPlaylist.Title.Should().Be("Playlist B");

            var addFriendHandler = new AddFriendCommandHandler(
                dbContext,
                NullLogger<AddFriendCommandHandler>.Instance);
            var addFriendResult = await addFriendHandler.Handle(new AddFriendCommand
            {
                UserId = seed.UserId,
                FriendId = seed.FriendId
            }, CancellationToken.None);
            addFriendResult.Status.Should().Be(AddFriendStatus.AlreadyFriends);

            var bulkUsersHandler = new BulkCreateUsersCommandHandler(
                dbContext,
                hasher,
                mapper,
                NullLogger<BulkCreateUsersCommandHandler>.Instance);
            var bulkUsersResult = await bulkUsersHandler.Handle(new BulkCreateUsersCommand
            {
                Commands = new List<CreateUserCommand>
                {
                    new()
                    {
                        Username = "bulk1",
                        Email = "bulk1@music.local",
                        Password = "password",
                        DisplayName = "Bulk User"
                    },
                    new()
                    {
                        Username = "new_user",
                        Email = "new_user@music.local",
                        Password = "password",
                        DisplayName = "Duplicate User"
                    }
                }
            }, CancellationToken.None);
            bulkUsersResult.TotalCount.Should().Be(2);

            var bulkAlbumsHandler = new BulkCreateAlbumsCommandHandler(
                dbContext,
                mapper,
                NullLogger<BulkCreateAlbumsCommandHandler>.Instance);
            var bulkAlbumsResult = await bulkAlbumsHandler.Handle(new BulkCreateAlbumsCommand
            {
                Commands = new List<CreateAlbumCommand>
                {
                    new()
                    {
                        Title = "Bulk Album",
                        ArtistId = seed.ArtistId,
                        CreatedById = seed.UserId,
                        ReleaseDate = DateTime.UtcNow.AddDays(-1),
                        Type = "Album",
                        Genres = new List<string> { "Rock" }
                    },
                    new()
                    {
                        Title = "Invalid Album",
                        ArtistId = Guid.NewGuid(),
                        CreatedById = seed.UserId,
                        ReleaseDate = DateTime.UtcNow.AddDays(-1),
                        Type = "Album",
                        Genres = new List<string> { "Rock" }
                    }
                }
            }, CancellationToken.None);
            bulkAlbumsResult.TotalCount.Should().Be(2);

            var albumByIdHandler = new GetAlbumByIdQueryHandler(dbContext, mapper);
            var albumById = await albumByIdHandler.Handle(new GetAlbumByIdQuery
            {
                AlbumId = seed.AlbumId
            }, CancellationToken.None);
            albumById.Should().NotBeNull();

            var albumsByArtistHandler = new GetAlbumsByArtistQueryHandler(dbContext, mapper);
            var albumsByArtist = await albumsByArtistHandler.Handle(new GetAlbumsByArtistQuery
            {
                ArtistId = seed.ArtistId
            }, CancellationToken.None);
            albumsByArtist.Should().NotBeEmpty();

            var recentAlbumsHandler = new GetRecentAlbumsQueryHandler(dbContext, mapper);
            var recentAlbums = await recentAlbumsHandler.Handle(new GetRecentAlbumsQuery
            {
                Days = 30
            }, CancellationToken.None);
            recentAlbums.Should().NotBeEmpty();

            var albumsQueryHandler = new GetAlbumsQueryHandler(dbContext);
            var albumsPage = await albumsQueryHandler.Handle(new GetAlbumsQuery
            {
                Page = 1,
                PageSize = 10,
                SortBy = "title",
                SortOrder = "asc",
                Genre = "Rock"
            }, CancellationToken.None);
            albumsPage.Items.Should().NotBeEmpty();

            var topArtistsHandler = new GetTopArtistsQueryHandler(dbContext);
            var topArtists = await topArtistsHandler.Handle(new GetTopArtistsQuery
            {
                Count = 5
            }, CancellationToken.None);
            topArtists.Should().NotBeEmpty();

            var artistsByGenreHandler = new GetArtistsByGenreQueryHandler(dbContext);
            var artistsByGenre = await artistsByGenreHandler.Handle(new GetArtistsByGenreQuery
            {
                Genre = "Rock"
            }, CancellationToken.None);
            artistsByGenre.Should().NotBeEmpty();

            var artistByIdHandler = new GetArtistByIdQueryHandler(dbContext);
            var artistById = await artistByIdHandler.Handle(new GetArtistByIdQuery
            {
                ArtistId = seed.ArtistId
            }, CancellationToken.None);
            artistById.Should().NotBeNull();

            var trackByIdHandler = new GetTrackByIdQueryHandler(dbContext, mapper);
            var trackById = await trackByIdHandler.Handle(new GetTrackByIdQuery
            {
                TrackId = seed.TrackId
            }, CancellationToken.None);
            trackById.Should().NotBeNull();

            var tracksByAlbumHandler = new GetTracksByAlbumQueryHandler(dbContext, mapper);
            var tracksByAlbum = await tracksByAlbumHandler.Handle(new GetTracksByAlbumQuery
            {
                AlbumId = seed.AlbumId
            }, CancellationToken.None);
            tracksByAlbum.Should().NotBeEmpty();

            var topTracksHandler = new GetTopTracksQueryHandler(dbContext);
            var topTracksAll = await topTracksHandler.Handle(new GetTopTracksQuery
            {
                Count = 5,
                TimeRange = "all"
            }, CancellationToken.None);
            topTracksAll.Should().NotBeEmpty();

            var topTracksWeek = await topTracksHandler.Handle(new GetTopTracksQuery
            {
                Count = 5,
                TimeRange = "week"
            }, CancellationToken.None);
            topTracksWeek.Should().NotBeEmpty();

            var playlistByIdHandler = new GetPlaylistByIdQueryHandler(dbContext, mapper);
            var playlistById = await playlistByIdHandler.Handle(new GetPlaylistByIdQuery
            {
                PlaylistId = seed.PlaylistId
            }, CancellationToken.None);
            playlistById.Should().NotBeNull();

            var userPlaylistsHandler = new GetUserPlaylistsQueryHandler(dbContext);
            var userPlaylists = await userPlaylistsHandler.Handle(new GetUserPlaylistsQuery
            {
                UserId = seed.UserId
            }, CancellationToken.None);
            userPlaylists.Should().NotBeEmpty();

            var userPlaylistsByUserHandler = new GetUserPlaylistsByUserIdQueryHandler(dbContext);
            var userPlaylistsByUser = await userPlaylistsByUserHandler.Handle(new GetUserPlaylistsByUserIdQuery
            {
                UserId = seed.UserId
            }, CancellationToken.None);
            userPlaylistsByUser.Should().NotBeEmpty();

            var publicPlaylistsHandler = new GetPublicPlaylistsQueryHandler(dbContext);
            var publicPlaylists = await publicPlaylistsHandler.Handle(new GetPublicPlaylistsQuery
            {
                SortBy = "title",
                SortOrder = "asc",
                Limit = 10
            }, CancellationToken.None);
            publicPlaylists.Should().NotBeEmpty();

            var usersQueryHandler = new GetUsersQueryHandler(dbContext);
            var usersPage = await usersQueryHandler.Handle(new GetUsersQuery
            {
                Page = 1,
                PageSize = 10,
                Country = "US"
            }, CancellationToken.None);
            usersPage.Items.Should().NotBeEmpty();

            var userByIdHandler = new GetUserByIdQueryHandler(dbContext);
            var userById = await userByIdHandler.Handle(new GetUserByIdQuery
            {
                UserId = seed.UserId
            }, CancellationToken.None);
            userById.Should().NotBeNull();

            var userStatsHandler = new GetUserStatisticsQueryHandler(
                dbContext,
                NullLogger<GetUserStatisticsQueryHandler>.Instance);
            var userStats = await userStatsHandler.Handle(new GetUserStatisticsQuery
            {
                UserId = seed.UserId,
                TimeRangeDays = 30
            }, CancellationToken.None);
            userStats.TotalPlaylists.Should().BeGreaterThan(0);

            var aiHandler = new GeneratePersonalPlaylistsQueryHandler(dbContext);
            var aiPlaylists = await aiHandler.Handle(new GeneratePersonalPlaylistsQuery
            {
                UserId = seed.UserId,
                Count = 2
            }, CancellationToken.None);
            aiPlaylists.Should().HaveCount(2);
        }

        [Fact]
        public async Task BulkDeleteAlbums_ShouldHandleSuccessAndFailure()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var seed = await TestDataSeeder.SeedAsync(dbContext);

            var handler = new BulkDeleteAlbumsCommandHandler(
                dbContext,
                NullLogger<BulkDeleteAlbumsCommandHandler>.Instance);

            var failed = await handler.Handle(new BulkDeleteAlbumsCommand
            {
                AlbumIds = new List<Guid> { seed.AlbumId, Guid.NewGuid() }
            }, CancellationToken.None);
            failed.FailedCount.Should().BeGreaterThan(0);
            failed.SuccessfulCount.Should().Be(1);

            var success = await handler.Handle(new BulkDeleteAlbumsCommand
            {
                AlbumIds = new List<Guid> { seed.AlbumId }
            }, CancellationToken.None);
            success.SuccessfulCount.Should().Be(0);
            success.FailedCount.Should().Be(1);
        }
    }
}
