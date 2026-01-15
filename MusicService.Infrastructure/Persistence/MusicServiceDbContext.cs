using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MusicService.Application.Common.Interfaces;
using MusicService.Domain.Entities;

namespace MusicService.Infrastructure.Persistence
{
    public class MusicServiceDbContext : DbContext, IMusicServiceDbContext
    {
        public MusicServiceDbContext(DbContextOptions<MusicServiceDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Artist> Artists => Set<Artist>();
        public DbSet<Album> Albums => Set<Album>();
        public DbSet<Track> Tracks => Set<Track>();
        public DbSet<Playlist> Playlists => Set<Playlist>();
        public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();
        public DbSet<ListenHistory> ListenHistories => Set<ListenHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && c1.SequenceEqual(c2),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? new List<string>() : new List<string>(c));

            modelBuilder.Entity<User>(builder =>
            {
                builder.ToTable("users");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Id).ValueGeneratedNever();
                builder.Property(x => x.Username).IsRequired().HasMaxLength(50);
                builder.Property(x => x.Email).IsRequired().HasMaxLength(100);
                builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(200);
                builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
                builder.Property(x => x.ProfileImage).HasMaxLength(500);
                builder.Property(x => x.Country).IsRequired().HasMaxLength(80).HasDefaultValue("Unknown");
                builder.Property(x => x.ListenTimeMinutes).HasDefaultValue(0);
                builder.Property(x => x.LastLogin).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.FavoriteGenres)
                    .HasColumnType("text[]")
                    .Metadata.SetValueComparer(stringListComparer);
                builder.HasIndex(x => x.Username).IsUnique();
                builder.HasIndex(x => x.Email).IsUnique();

                builder.HasMany(x => x.CreatedPlaylists)
                    .WithOne(x => x.CreatedBy)
                    .HasForeignKey(x => x.CreatedById)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(x => x.ListenHistory)
                    .WithOne(x => x.User)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Role>(builder =>
            {
                builder.ToTable("roles");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Id).ValueGeneratedNever();
                builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
                builder.Property(x => x.Description).HasMaxLength(200);
                builder.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.HasIndex(x => x.Name).IsUnique();
            });

            modelBuilder.Entity<UserRole>(builder =>
            {
                builder.ToTable("user_roles");
                builder.HasKey(x => new { x.UserId, x.RoleId });
                builder.Property(x => x.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.HasOne(x => x.User)
                    .WithMany(x => x.UserRoles)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                builder.HasOne(x => x.Role)
                    .WithMany(x => x.UserRoles)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Artist>(builder =>
            {
                builder.ToTable("artists");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Id).ValueGeneratedNever();
                builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
                builder.Property(x => x.RealName).HasMaxLength(120);
                builder.Property(x => x.Biography).HasMaxLength(2000);
                builder.Property(x => x.ProfileImage).HasMaxLength(500);
                builder.Property(x => x.CoverImage).HasMaxLength(500);
                builder.Property(x => x.Country).IsRequired().HasMaxLength(80).HasDefaultValue("Unknown");
                builder.Property(x => x.IsVerified).HasDefaultValue(false);
                builder.Property(x => x.MonthlyListeners).HasDefaultValue(0);
                builder.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.Genres)
                    .HasColumnType("text[]")
                    .Metadata.SetValueComparer(stringListComparer);
                builder.HasIndex(x => x.Name).IsUnique();
                builder.Ignore(x => x.YearsActive);

                builder.HasMany(x => x.Albums)
                    .WithOne(x => x.Artist)
                    .HasForeignKey(x => x.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(x => x.Tracks)
                    .WithOne(x => x.Artist)
                    .HasForeignKey(x => x.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Album>(builder =>
            {
                builder.ToTable("albums");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Id).ValueGeneratedNever();
                builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
                builder.Property(x => x.Description).HasMaxLength(2000);
                builder.Property(x => x.CoverImage).HasMaxLength(500);
                builder.Property(x => x.ReleaseDate).IsRequired();
                builder.Property(x => x.Type).IsRequired();
                builder.Property(x => x.TotalDurationMinutes).HasDefaultValue(0);
                builder.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.Genres)
                    .HasColumnType("text[]")
                    .Metadata.SetValueComparer(stringListComparer);
                builder.HasIndex(x => new { x.ArtistId, x.Title }).IsUnique();
                builder.Ignore(x => x.TrackCount);
                builder.Ignore(x => x.IsSingle);

                builder.HasMany(x => x.Tracks)
                    .WithOne(x => x.Album)
                    .HasForeignKey(x => x.AlbumId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Track>(builder =>
            {
                builder.ToTable("tracks");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Id).ValueGeneratedNever();
                builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
                builder.Property(x => x.DurationSeconds).IsRequired();
                builder.Property(x => x.Lyrics).HasMaxLength(10000);
                builder.Property(x => x.AudioFileUrl).HasMaxLength(500);
                builder.Property(x => x.TrackNumber).IsRequired();
                builder.Property(x => x.PlayCount).HasDefaultValue(0);
                builder.Property(x => x.LikeCount).HasDefaultValue(0);
                builder.Property(x => x.IsExplicit).HasDefaultValue(false);
                builder.Property(x => x.PopularityScore).HasPrecision(5, 2).HasDefaultValue(0);
                builder.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.HasIndex(x => new { x.AlbumId, x.TrackNumber }).IsUnique();
                builder.HasIndex(x => x.PlayCount);
                builder.Ignore(x => x.DurationFormatted);

                builder.HasMany(x => x.PlaylistTracks)
                    .WithOne(x => x.Track)
                    .HasForeignKey(x => x.TrackId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(x => x.ListenHistory)
                    .WithOne(x => x.Track)
                    .HasForeignKey(x => x.TrackId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Playlist>(builder =>
            {
                builder.ToTable("playlists");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Id).ValueGeneratedNever();
                builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
                builder.Property(x => x.Description).HasMaxLength(2000);
                builder.Property(x => x.CoverImage).HasMaxLength(500);
                builder.Property(x => x.IsPublic).HasDefaultValue(true);
                builder.Property(x => x.IsCollaborative).HasDefaultValue(false);
                builder.Property(x => x.Type).IsRequired();
                builder.Property(x => x.FollowersCount).HasDefaultValue(0);
                builder.Property(x => x.TotalDurationMinutes).HasDefaultValue(0);
                builder.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.HasIndex(x => new { x.CreatedById, x.Title }).IsUnique();
                builder.Ignore(x => x.TrackCount);

                builder.HasMany(x => x.PlaylistTracks)
                    .WithOne(x => x.Playlist)
                    .HasForeignKey(x => x.PlaylistId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlaylistTrack>(builder =>
            {
                builder.ToTable("playlist_tracks");
                builder.HasKey(x => new { x.PlaylistId, x.TrackId });
                builder.Property(x => x.Position).IsRequired();
                builder.Property(x => x.AddedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                builder.HasOne(x => x.Playlist)
                    .WithMany(x => x.PlaylistTracks)
                    .HasForeignKey(x => x.PlaylistId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(x => x.Track)
                    .WithMany(x => x.PlaylistTracks)
                    .HasForeignKey(x => x.TrackId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(x => x.AddedBy)
                    .WithMany()
                    .HasForeignKey(x => x.AddedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ListenHistory>(builder =>
            {
                builder.ToTable("listen_history");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Id).ValueGeneratedNever();
                builder.Property(x => x.ListenedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.ListenDurationSeconds).IsRequired();
                builder.Property(x => x.Device).HasMaxLength(100);
                builder.Property(x => x.Completed).HasDefaultValue(false);
                builder.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                builder.HasIndex(x => new { x.UserId, x.ListenedAt });
                builder.Ignore(x => x.WasListenedToday);
            });

            modelBuilder.Entity<User>()
                .HasMany(x => x.FollowedPlaylists)
                .WithMany(x => x.Followers)
                .UsingEntity<Dictionary<string, object>>(
                    "playlist_followers",
                    right => right.HasOne<Playlist>().WithMany().HasForeignKey("playlist_id").OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<User>().WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("user_id", "playlist_id");
                    });

            modelBuilder.Entity<User>()
                .HasMany(x => x.FavoriteTracks)
                .WithMany(x => x.LikedByUsers)
                .UsingEntity<Dictionary<string, object>>(
                    "user_favorite_tracks",
                    right => right.HasOne<Track>().WithMany().HasForeignKey("track_id").OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<User>().WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("user_id", "track_id");
                    });

            modelBuilder.Entity<User>()
                .HasMany(x => x.FollowedArtists)
                .WithMany(x => x.Followers)
                .UsingEntity<Dictionary<string, object>>(
                    "user_followed_artists",
                    right => right.HasOne<Artist>().WithMany().HasForeignKey("artist_id").OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<User>().WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("user_id", "artist_id");
                    });

            modelBuilder.Entity<User>()
                .HasMany(x => x.FavoriteAlbums)
                .WithMany(x => x.AddedByUsers)
                .UsingEntity<Dictionary<string, object>>(
                    "user_favorite_albums",
                    right => right.HasOne<Album>().WithMany().HasForeignKey("album_id").OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<User>().WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("user_id", "album_id");
                    });

            modelBuilder.Entity<User>()
                .HasMany(x => x.Friends)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "user_friends",
                    right => right.HasOne<User>().WithMany().HasForeignKey("friend_id").OnDelete(DeleteBehavior.Restrict),
                    left => left.HasOne<User>().WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("user_id", "friend_id");
                    });

            SeedData(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.Id == Guid.Empty)
                    {
                        entry.Entity.Id = Guid.NewGuid();
                    }
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            var adminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var demoUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            var seedTime = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = adminRoleId, Name = "Admin", Description = "System administrator", CreatedAt = seedTime, UpdatedAt = seedTime },
                new Role { Id = userRoleId, Name = "User", Description = "Default user", CreatedAt = seedTime, UpdatedAt = seedTime }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = adminUserId,
                    Username = "admin",
                    Email = "admin@music.local",
                    PasswordHash = "demo",
                    DisplayName = "Administrator",
                    Country = "Unknown",
                    ListenTimeMinutes = 0,
                    LastLogin = seedTime,
                    CreatedAt = seedTime,
                    UpdatedAt = seedTime,
                    FavoriteGenres = new List<string>()
                },
                new User
                {
                    Id = demoUserId,
                    Username = "demo",
                    Email = "demo@music.local",
                    PasswordHash = "demo",
                    DisplayName = "Demo User",
                    Country = "Unknown",
                    ListenTimeMinutes = 0,
                    LastLogin = seedTime,
                    CreatedAt = seedTime,
                    UpdatedAt = seedTime,
                    FavoriteGenres = new List<string>()
                }
            );

            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = adminUserId, RoleId = adminRoleId, AssignedAt = seedTime },
                new UserRole { UserId = demoUserId, RoleId = userRoleId, AssignedAt = seedTime }
            );
        }
    }
}
