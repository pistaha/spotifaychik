using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Persistence;

namespace Tests.EFCoreTests
{
    public sealed record SeedData(
        Guid UserId,
        Guid FriendId,
        Guid ArtistId,
        Guid AlbumId,
        Guid TrackId,
        Guid PlaylistId);

    public static class TestDataSeeder
    {
        public static async Task<SeedData> SeedAsync(MusicServiceDbContext dbContext)
        {
            var now = DateTime.UtcNow;

            var userId = Guid.NewGuid();
            var friendId = Guid.NewGuid();
            var artistId = Guid.NewGuid();
            var albumId = Guid.NewGuid();
            var trackId = Guid.NewGuid();
            var playlistId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "user1",
                Email = "user1@music.local",
                PasswordHash = "hash",
                DisplayName = "User One",
                Country = "US",
                FavoriteGenres = new List<string> { "Rock", "Pop" },
                LastLoginAt = now.AddDays(-2)
            };

            var friend = new User
            {
                Id = friendId,
                Username = "user2",
                Email = "user2@music.local",
                PasswordHash = "hash",
                DisplayName = "User Two",
                Country = "US",
                FavoriteGenres = new List<string> { "Jazz" },
                LastLoginAt = now.AddDays(-1)
            };

            var artist = new Artist
            {
                Id = artistId,
                Name = "Artist A",
                Country = "US",
                Genres = new List<string> { "Rock" },
                MonthlyListeners = 500,
                CareerStartDate = now.AddYears(-5)
            };

            var album = new Album
            {
                Id = albumId,
                Title = "Album A",
                ReleaseDate = now.AddDays(-10),
                Type = AlbumType.Album,
                Genres = new List<string> { "Rock" },
                ArtistId = artistId,
                Artist = artist
            };

            var track = new Track
            {
                Id = trackId,
                Title = "Track A",
                DurationSeconds = 180,
                TrackNumber = 1,
                PlayCount = 10,
                LikeCount = 2,
                AlbumId = albumId,
                Album = album,
                ArtistId = artistId,
                Artist = artist
            };

            var playlist = new Playlist
            {
                Id = playlistId,
                Title = "Playlist A",
                Type = PlaylistType.UserCreated,
                IsPublic = true,
                IsCollaborative = false,
                FollowersCount = 3,
                TotalDurationMinutes = 10,
                CreatedById = userId,
                CreatedBy = user
            };

            var playlistTrack = new PlaylistTrack
            {
                Id = Guid.NewGuid(),
                PlaylistId = playlistId,
                Playlist = playlist,
                TrackId = trackId,
                Track = track,
                Position = 1,
                AddedById = userId,
                AddedBy = user
            };

            var listenHistory = new ListenHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = user,
                TrackId = trackId,
                Track = track,
                ListenedAt = now.AddDays(-1),
                ListenDurationSeconds = 180,
                Completed = true
            };

            user.CreatedPlaylists.Add(playlist);
            user.Friends.Add(friend);
            user.FollowedArtists.Add(artist);
            user.FollowedPlaylists.Add(playlist);
            user.ListenHistory.Add(listenHistory);

            artist.Albums.Add(album);
            artist.Tracks.Add(track);

            album.Tracks.Add(track);

            playlist.PlaylistTracks.Add(playlistTrack);
            track.PlaylistTracks.Add(playlistTrack);
            track.ListenHistory.Add(listenHistory);

            dbContext.Users.AddRange(user, friend);
            dbContext.Artists.Add(artist);
            dbContext.Albums.Add(album);
            dbContext.Tracks.Add(track);
            dbContext.Playlists.Add(playlist);
            dbContext.PlaylistTracks.Add(playlistTrack);
            dbContext.ListenHistories.Add(listenHistory);

            await dbContext.SaveChangesAsync();

            return new SeedData(userId, friendId, artistId, albumId, trackId, playlistId);
        }
    }
}
