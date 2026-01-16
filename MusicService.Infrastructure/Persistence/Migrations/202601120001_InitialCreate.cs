using FluentMigrator;
using System.Data;

namespace MusicService.Infrastructure.Persistence.Migrations
{
    [Migration(202601120001)]
    public class InitialCreate : Migration
    {
        public override void Up()
        {
            Create.Table("roles")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("Name").AsString(50).NotNullable().Unique()
                .WithColumn("Description").AsString(200).Nullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

            Create.Table("users")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("Username").AsString(50).NotNullable()
                .WithColumn("Email").AsString(100).NotNullable()
                .WithColumn("PasswordHash").AsString(200).NotNullable()
                .WithColumn("DisplayName").AsString(100).NotNullable()
                .WithColumn("ProfileImage").AsString(500).Nullable()
                .WithColumn("DateOfBirth").AsDateTime().Nullable()
                .WithColumn("Country").AsString(80).NotNullable().WithDefaultValue("Unknown")
                .WithColumn("FavoriteGenres").AsCustom("text[]").NotNullable().WithDefaultValue("{}")
                .WithColumn("ListenTimeMinutes").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("LastLogin").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

            Create.Index("IX_users_username").OnTable("users").OnColumn("Username").Ascending().WithOptions().Unique();
            Create.Index("IX_users_email").OnTable("users").OnColumn("Email").Ascending().WithOptions().Unique();

            Create.Table("user_roles")
                .WithColumn("UserId").AsGuid().NotNullable().ForeignKey("users", "Id")
                .WithColumn("RoleId").AsGuid().NotNullable().ForeignKey("roles", "Id")
                .WithColumn("AssignedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

            Create.PrimaryKey("PK_user_roles").OnTable("user_roles").Columns("UserId", "RoleId");

            Create.Table("artists")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("Name").AsString(120).NotNullable()
                .WithColumn("RealName").AsString(120).Nullable()
                .WithColumn("Biography").AsString(2000).Nullable()
                .WithColumn("ProfileImage").AsString(500).Nullable()
                .WithColumn("CoverImage").AsString(500).Nullable()
                .WithColumn("Genres").AsCustom("text[]").NotNullable().WithDefaultValue("{}")
                .WithColumn("Country").AsString(80).NotNullable().WithDefaultValue("Unknown")
                .WithColumn("CareerStartDate").AsDateTime().Nullable()
                .WithColumn("IsVerified").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("MonthlyListeners").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

            Create.Index("IX_artists_name").OnTable("artists").OnColumn("Name").Ascending().WithOptions().Unique();

            Create.Table("albums")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("Title").AsString(200).NotNullable()
                .WithColumn("Description").AsString(2000).Nullable()
                .WithColumn("CoverImage").AsString(500).Nullable()
                .WithColumn("ReleaseDate").AsDateTime().NotNullable()
                .WithColumn("Type").AsInt32().NotNullable()
                .WithColumn("Genres").AsCustom("text[]").NotNullable().WithDefaultValue("{}")
                .WithColumn("TotalDurationMinutes").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("ArtistId").AsGuid().NotNullable().ForeignKey("artists", "Id")
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

            Create.Index("IX_albums_artist_title").OnTable("albums").OnColumn("ArtistId").Ascending().OnColumn("Title").Ascending().WithOptions().Unique();

            Create.Table("tracks")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("Title").AsString(200).NotNullable()
                .WithColumn("DurationSeconds").AsInt32().NotNullable()
                .WithColumn("Lyrics").AsString(10000).Nullable()
                .WithColumn("AudioFileUrl").AsString(500).Nullable()
                .WithColumn("TrackNumber").AsInt32().NotNullable()
                .WithColumn("PlayCount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("LikeCount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("IsExplicit").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("PopularityScore").AsDecimal(5, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("AlbumId").AsGuid().NotNullable().ForeignKey("albums", "Id")
                .WithColumn("ArtistId").AsGuid().NotNullable().ForeignKey("artists", "Id")
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

            Create.Index("IX_tracks_album_tracknumber").OnTable("tracks").OnColumn("AlbumId").Ascending().OnColumn("TrackNumber").Ascending().WithOptions().Unique();
            Create.Index("IX_tracks_playcount").OnTable("tracks").OnColumn("PlayCount");

            Create.Table("playlists")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("Title").AsString(200).NotNullable()
                .WithColumn("Description").AsString(2000).Nullable()
                .WithColumn("CoverImage").AsString(500).Nullable()
                .WithColumn("IsPublic").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("IsCollaborative").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("Type").AsInt32().NotNullable()
                .WithColumn("FollowersCount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("TotalDurationMinutes").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("CreatedById").AsGuid().NotNullable().ForeignKey("users", "Id")
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

            Create.Index("IX_playlists_creator_title").OnTable("playlists").OnColumn("CreatedById").Ascending().OnColumn("Title").Ascending().WithOptions().Unique();

            Create.Table("playlist_tracks")
                .WithColumn("PlaylistId").AsGuid().NotNullable().ForeignKey("playlists", "Id")
                .WithColumn("TrackId").AsGuid().NotNullable().ForeignKey("tracks", "Id")
                .WithColumn("Position").AsInt32().NotNullable()
                .WithColumn("AddedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("AddedById").AsGuid().Nullable();

            Create.PrimaryKey("PK_playlist_tracks").OnTable("playlist_tracks").Columns("PlaylistId", "TrackId");

            Create.Table("listen_history")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("UserId").AsGuid().NotNullable().ForeignKey("users", "Id")
                .WithColumn("TrackId").AsGuid().NotNullable().ForeignKey("tracks", "Id")
                .WithColumn("ListenedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("ListenDurationSeconds").AsInt32().NotNullable()
                .WithColumn("Device").AsString(100).Nullable()
                .WithColumn("Completed").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

            Create.Index("IX_listen_history_user_listenedat").OnTable("listen_history")
                .OnColumn("UserId").Ascending()
                .OnColumn("ListenedAt").Descending();

            Create.Table("playlist_followers")
                .WithColumn("user_id").AsGuid().NotNullable().ForeignKey("users", "Id")
                .WithColumn("playlist_id").AsGuid().NotNullable().ForeignKey("playlists", "Id");

            Create.PrimaryKey("PK_playlist_followers").OnTable("playlist_followers").Columns("user_id", "playlist_id");

            Create.Table("user_favorite_tracks")
                .WithColumn("user_id").AsGuid().NotNullable().ForeignKey("users", "Id")
                .WithColumn("track_id").AsGuid().NotNullable().ForeignKey("tracks", "Id");

            Create.PrimaryKey("PK_user_favorite_tracks").OnTable("user_favorite_tracks").Columns("user_id", "track_id");

            Create.Table("user_followed_artists")
                .WithColumn("user_id").AsGuid().NotNullable().ForeignKey("users", "Id")
                .WithColumn("artist_id").AsGuid().NotNullable().ForeignKey("artists", "Id");

            Create.PrimaryKey("PK_user_followed_artists").OnTable("user_followed_artists").Columns("user_id", "artist_id");

            Create.Table("user_favorite_albums")
                .WithColumn("user_id").AsGuid().NotNullable().ForeignKey("users", "Id")
                .WithColumn("album_id").AsGuid().NotNullable().ForeignKey("albums", "Id");

            Create.PrimaryKey("PK_user_favorite_albums").OnTable("user_favorite_albums").Columns("user_id", "album_id");

            Create.Table("user_friends")
                .WithColumn("user_id").AsGuid().NotNullable()
                .WithColumn("friend_id").AsGuid().NotNullable();

            Create.PrimaryKey("PK_user_friends").OnTable("user_friends").Columns("user_id", "friend_id");

            Create.ForeignKey("FK_playlist_tracks_added_by")
                .FromTable("playlist_tracks").ForeignColumn("AddedById")
                .ToTable("users").PrimaryColumn("Id")
                .OnDeleteOrUpdate(Rule.SetNull);

            Create.ForeignKey("FK_user_friends_user")
                .FromTable("user_friends").ForeignColumn("user_id")
                .ToTable("users").PrimaryColumn("Id")
                .OnDeleteOrUpdate(Rule.Cascade);

            Create.ForeignKey("FK_user_friends_friend")
                .FromTable("user_friends").ForeignColumn("friend_id")
                .ToTable("users").PrimaryColumn("Id")
                .OnDeleteOrUpdate(Rule.None);
        }

        public override void Down()
        {
            Delete.Table("user_friends");
            Delete.Table("user_favorite_albums");
            Delete.Table("user_followed_artists");
            Delete.Table("user_favorite_tracks");
            Delete.Table("playlist_followers");
            Delete.Table("listen_history");
            Delete.Table("playlist_tracks");
            Delete.Table("playlists");
            Delete.Table("tracks");
            Delete.Table("albums");
            Delete.Table("artists");
            Delete.Table("user_roles");
            Delete.Table("users");
            Delete.Table("roles");
        }
    }
}
  