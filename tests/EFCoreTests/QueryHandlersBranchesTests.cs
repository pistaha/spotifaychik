using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MusicService.Application.Albums.Queries;
using MusicService.Application.Playlists.Queries;
using MusicService.Application.Users.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.EFCoreTests
{
    public class QueryHandlersBranchesTests
    {
        [Fact]
        public async Task GetPublicPlaylists_ShouldHandleSortingAndLimit()
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
            dbContext.Playlists.AddRange(
                new Playlist { Id = Guid.NewGuid(), Title = "B", CreatedById = user.Id, FollowersCount = 10, IsPublic = true, Type = PlaylistType.UserCreated },
                new Playlist { Id = Guid.NewGuid(), Title = "A", CreatedById = user.Id, FollowersCount = 5, IsPublic = true, Type = PlaylistType.UserCreated }
            );
            await dbContext.SaveChangesAsync();

            var handler = new GetPublicPlaylistsQueryHandler(dbContext);

            var byFollowers = await handler.Handle(new GetPublicPlaylistsQuery
            {
                SortBy = "followerscount",
                SortOrder = "asc",
                Limit = 1
            }, CancellationToken.None);

            byFollowers.Should().HaveCount(1);
            byFollowers.First().FollowersCount.Should().Be(5);

            var byCreated = await handler.Handle(new GetPublicPlaylistsQuery
            {
                SortBy = "createdat",
                SortOrder = "desc"
            }, CancellationToken.None);
            byCreated.Should().HaveCount(2);

            var byDefault = await handler.Handle(new GetPublicPlaylistsQuery
            {
                SortBy = "unknown",
                SortOrder = "desc"
            }, CancellationToken.None);
            byDefault.Should().HaveCount(2);

            var byTitle = await handler.Handle(new GetPublicPlaylistsQuery
            {
                SortBy = "title",
                SortOrder = "asc"
            }, CancellationToken.None);
            byTitle.First().Title.Should().Be("A");

            var byFollowersDesc = await handler.Handle(new GetPublicPlaylistsQuery
            {
                SortBy = "followerscount",
                SortOrder = "desc"
            }, CancellationToken.None);
            byFollowersDesc.First().FollowersCount.Should().Be(10);
        }

        [Fact]
        public async Task GetAlbums_ShouldHandleSortingBranches()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var artist = new Artist { Id = Guid.NewGuid(), Name = "Artist", Country = "US" };
            dbContext.Artists.Add(artist);
            dbContext.Albums.AddRange(
                new Album { Id = Guid.NewGuid(), Title = "B", ArtistId = artist.Id, ReleaseDate = DateTime.UtcNow.AddDays(-1), Type = AlbumType.Album },
                new Album { Id = Guid.NewGuid(), Title = "A", ArtistId = artist.Id, ReleaseDate = DateTime.UtcNow.AddDays(-2), Type = AlbumType.Album }
            );
            await dbContext.SaveChangesAsync();

            var handler = new GetAlbumsQueryHandler(dbContext);

            var byTitle = await handler.Handle(new GetAlbumsQuery
            {
                Page = 1,
                PageSize = 10,
                SortBy = "title",
                SortOrder = "asc"
            }, CancellationToken.None);
            byTitle.Items.First().Title.Should().Be("A");

            var byRelease = await handler.Handle(new GetAlbumsQuery
            {
                Page = 1,
                PageSize = 10,
                SortBy = "releasedate",
                SortOrder = "desc"
            }, CancellationToken.None);
            byRelease.Items.Should().HaveCount(2);

            var byDefault = await handler.Handle(new GetAlbumsQuery
            {
                Page = 1,
                PageSize = 10,
                SortOrder = "asc"
            }, CancellationToken.None);
            byDefault.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetUsers_ShouldHandleSearchAndCountry()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            dbContext.Users.AddRange(
                new User { Id = Guid.NewGuid(), Username = "john", Email = "john@music.local", PasswordHash = "hash", DisplayName = "John", Country = "US" },
                new User { Id = Guid.NewGuid(), Username = "mike", Email = "mike@music.local", PasswordHash = "hash", DisplayName = "Mike", Country = "DE" }
            );
            await dbContext.SaveChangesAsync();

            var handler = new GetUsersQueryHandler(dbContext);
            var result = await handler.Handle(new GetUsersQuery
            {
                Page = 1,
                PageSize = 10,
                Search = "john",
                Country = "US"
            }, CancellationToken.None);

            result.Items.Should().ContainSingle(u => u.Username == "john");
        }
    }
}
