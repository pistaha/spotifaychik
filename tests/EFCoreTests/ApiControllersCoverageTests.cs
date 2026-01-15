using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MusicService.API.Controllers;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Albums.Queries;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Artists.Queries;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Playlists.Commands;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Playlists.Queries;
using MusicService.Application.Search.Dtos;
using MusicService.Application.Search.Queries;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Tracks.Queries;
using MusicService.Application.Users.Commands;
using MusicService.Application.Users.Dtos;
using MusicService.Application.Users.Queries;
using MusicService.API.Authentication;
using MusicService.API.Models;
using Microsoft.Extensions.Options;
using Xunit;

namespace Tests.EFCoreTests
{
    public class ApiControllersCoverageTests
    {
        [Fact]
        public async Task AlbumsController_ShouldHandleEndpoints()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GetAlbumByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AlbumDto?)null);
            mediator.Setup(m => m.Send(It.IsAny<CreateAlbumCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AlbumDto { Id = Guid.NewGuid(), Title = "Album" });
            mediator.Setup(m => m.Send(It.IsAny<BulkCreateAlbumsCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BulkOperationResult<AlbumDto>());
            mediator.Setup(m => m.Send(It.IsAny<BulkDeleteAlbumsCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BulkDeleteResult());
            mediator.Setup(m => m.Send(It.IsAny<GetAlbumsByArtistQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AlbumDto>());
            mediator.Setup(m => m.Send(It.IsAny<GetRecentAlbumsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AlbumDto>());
            mediator.Setup(m => m.Send(It.IsAny<GetAlbumsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<AlbumDto>(new List<AlbumDto>(), 0, 1, 10));

            var controller = new AlbumsController(mediator.Object);
            var notFound = await controller.GetAlbum(Guid.NewGuid(), CancellationToken.None);
            notFound.Result.Should().BeOfType<NotFoundObjectResult>();

            var created = await controller.CreateAlbum(new CreateAlbumCommand(), CancellationToken.None);
            created.Result.Should().BeOfType<CreatedAtActionResult>();

            var bulkCreate = await controller.BulkCreateAlbums(new List<CreateAlbumCommand>(), CancellationToken.None);
            bulkCreate.Result.Should().BeOfType<OkObjectResult>();

            var bulkDelete = await controller.BulkDeleteAlbums(new List<Guid>(), CancellationToken.None);
            bulkDelete.Result.Should().BeOfType<OkObjectResult>();

            var byArtist = await controller.GetAlbumsByArtist(Guid.NewGuid(), CancellationToken.None);
            byArtist.Result.Should().BeOfType<OkObjectResult>();

            var recent = await controller.GetRecentAlbums(30, CancellationToken.None);
            recent.Result.Should().BeOfType<OkObjectResult>();

            var albums = await controller.GetAlbums(1, 10, null, null, "CreatedAt", "desc", CancellationToken.None);
            albums.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ArtistsController_ShouldHandleEndpoints()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GetArtistByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ArtistDto?)null);
            mediator.Setup(m => m.Send(It.IsAny<CreateArtistCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ArtistDto { Id = Guid.NewGuid(), Name = "Artist" });
            mediator.Setup(m => m.Send(It.IsAny<GetTopArtistsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ArtistDto>());
            mediator.Setup(m => m.Send(It.IsAny<GetArtistsByGenreQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ArtistDto>());

            var controller = new ArtistsController(mediator.Object);
            var notFound = await controller.GetArtist(Guid.NewGuid(), CancellationToken.None);
            notFound.Result.Should().BeOfType<NotFoundObjectResult>();

            var created = await controller.CreateArtist(new CreateArtistCommand(), CancellationToken.None);
            created.Result.Should().BeOfType<CreatedAtActionResult>();

            var top = await controller.GetTopArtists(5, CancellationToken.None);
            top.Result.Should().BeOfType<OkObjectResult>();

            var byGenre = await controller.GetArtistsByGenre("rock", CancellationToken.None);
            byGenre.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task TracksController_ShouldHandleEndpoints()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GetTrackByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TrackDto?)null);
            mediator.Setup(m => m.Send(It.IsAny<GetTracksByAlbumQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TrackDto>());
            mediator.Setup(m => m.Send(It.IsAny<GetTopTracksQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TrackDto>());

            var controller = new TracksController(mediator.Object);
            var notFound = await controller.GetTrack(Guid.NewGuid(), CancellationToken.None);
            notFound.Result.Should().BeOfType<NotFoundObjectResult>();

            var byAlbum = await controller.GetTracksByAlbum(Guid.NewGuid(), CancellationToken.None);
            byAlbum.Result.Should().BeOfType<OkObjectResult>();

            var top = await controller.GetTopTracks(5, CancellationToken.None);
            top.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task PlaylistsController_ShouldHandleEndpoints()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GetPlaylistByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlaylistDto?)null);
            mediator.Setup(m => m.Send(It.IsAny<CreatePlaylistCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaylistDto { Id = Guid.NewGuid(), Title = "Playlist" });
            mediator.Setup(m => m.Send(It.IsAny<GetUserPlaylistsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PlaylistDto>());
            mediator.Setup(m => m.Send(It.IsAny<GetPublicPlaylistsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PlaylistDto>());

            var controller = new PlaylistsController(mediator.Object);
            var notFound = await controller.GetPlaylist(Guid.NewGuid(), null, CancellationToken.None);
            notFound.Result.Should().BeOfType<NotFoundObjectResult>();

            var created = await controller.CreatePlaylist(new CreatePlaylistCommand(), CancellationToken.None);
            created.Result.Should().BeOfType<CreatedAtActionResult>();

            var userPlaylists = await controller.GetUserPlaylists(Guid.NewGuid(), CancellationToken.None);
            userPlaylists.Result.Should().BeOfType<OkObjectResult>();

            var publicPlaylists = await controller.GetPublicPlaylists(CancellationToken.None);
            publicPlaylists.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UsersController_ShouldHandleEndpoints()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserDto?)null);
            mediator.Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserDto { Id = Guid.NewGuid(), Username = "user" });
            mediator.Setup(m => m.Send(It.IsAny<BulkCreateUsersCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BulkOperationResult<UserDto>());
            mediator.Setup(m => m.Send(It.IsAny<GetUserPlaylistsByUserIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PlaylistDto>());
            mediator.Setup(m => m.Send(It.IsAny<AddFriendCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AddFriendResult.Ok());
            mediator.Setup(m => m.Send(It.IsAny<GetUserStatisticsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserStatisticsDto());
            mediator.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<UserDto>(new List<UserDto>(), 0, 1, 10));

            var jwtOptions = Options.Create(new JwtSettings
            {
                Issuer = "MusicService",
                Audience = "MusicServiceUsers",
                SecretKey = "test-secret-key-min-32-chars-long"
            });
            var tokenService = new JwtTokenService(jwtOptions);
            var controller = new UsersController(mediator.Object, tokenService, jwtOptions);
            var notFound = await controller.GetUser(Guid.NewGuid(), CancellationToken.None);
            notFound.Result.Should().BeOfType<NotFoundObjectResult>();

            var created = await controller.CreateUser(new CreateUserCommand(), CancellationToken.None);
            created.Result.Should().BeOfType<CreatedAtActionResult>();

            var bulk = await controller.BulkCreateUsers(new List<CreateUserCommand>(), CancellationToken.None);
            bulk.Result.Should().BeOfType<OkObjectResult>();

            var playlists = await controller.GetUserPlaylists(Guid.NewGuid(), CancellationToken.None);
            playlists.Result.Should().BeOfType<OkObjectResult>();

            var addFriend = await controller.AddFriend(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
            addFriend.Result.Should().BeOfType<OkObjectResult>();

            var stats = await controller.GetUserStatistics(Guid.NewGuid(), CancellationToken.None);
            stats.Result.Should().BeOfType<OkObjectResult>();

            var users = await controller.GetUsers(1, 10, null, null, CancellationToken.None);
            users.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UsersController_AddFriend_ShouldReturnNotFound_WhenUserMissing()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<AddFriendCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AddFriendResult.UserMissing());

            var jwtOptions = Options.Create(new JwtSettings
            {
                Issuer = "MusicService",
                Audience = "MusicServiceUsers",
                SecretKey = "test-secret-key-min-32-chars-long"
            });
            var tokenService = new JwtTokenService(jwtOptions);
            var controller = new UsersController(mediator.Object, tokenService, jwtOptions);
            var result = await controller.AddFriend(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UsersController_AddFriend_ShouldReturnNotFound_WhenFriendMissing()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<AddFriendCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AddFriendResult.FriendMissing());

            var jwtOptions = Options.Create(new JwtSettings
            {
                Issuer = "MusicService",
                Audience = "MusicServiceUsers",
                SecretKey = "test-secret-key-min-32-chars-long"
            });
            var tokenService = new JwtTokenService(jwtOptions);
            var controller = new UsersController(mediator.Object, tokenService, jwtOptions);
            var result = await controller.AddFriend(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UsersController_AddFriend_ShouldReturnConflict_WhenAlreadyFriends()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<AddFriendCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AddFriendResult.AlreadyFriends());

            var jwtOptions = Options.Create(new JwtSettings
            {
                Issuer = "MusicService",
                Audience = "MusicServiceUsers",
                SecretKey = "test-secret-key-min-32-chars-long"
            });
            var tokenService = new JwtTokenService(jwtOptions);
            var controller = new UsersController(mediator.Object, tokenService, jwtOptions);
            var result = await controller.AddFriend(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

            result.Result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task UsersController_AddFriend_ShouldReturnBadRequest_WhenFailed()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<AddFriendCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AddFriendResult.Failed());

            var jwtOptions = Options.Create(new JwtSettings
            {
                Issuer = "MusicService",
                Audience = "MusicServiceUsers",
                SecretKey = "test-secret-key-min-32-chars-long"
            });
            var tokenService = new JwtTokenService(jwtOptions);
            var controller = new UsersController(mediator.Object, tokenService, jwtOptions);
            var result = await controller.AddFriend(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task SearchController_ShouldHandleEndpoints()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchResultDto());
            mediator.Setup(m => m.Send(It.IsAny<AdvancedSearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<AdvancedSearchResultDto>(new List<AdvancedSearchResultDto>(), 0, 1, 10));
            mediator.Setup(m => m.Send(It.IsAny<GlobalSearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GlobalSearchResultDto());

            var controller = new SearchController(mediator.Object);
            var search = await controller.Search("query", null, 10, 0, CancellationToken.None);
            search.Result.Should().BeOfType<OkObjectResult>();

            var advanced = await controller.AdvancedSearch(new AdvancedPaginationRequest(), CancellationToken.None);
            advanced.Result.Should().BeOfType<OkObjectResult>();

            var global = await controller.GlobalSearch("query", 5, CancellationToken.None);
            global.Result.Should().BeOfType<OkObjectResult>();
        }
    }
}
