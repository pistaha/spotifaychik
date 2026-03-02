using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MusicService.Domain.Entities;

namespace MusicService.Application.Common.Interfaces
{
    public interface IMusicServiceDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<UserSession> UserSessions { get; }
        DbSet<UserClaim> UserClaims { get; }
        DbSet<Permission> Permissions { get; }
        DbSet<RolePermission> RolePermissions { get; }
        DbSet<SecurityAuditLog> SecurityAuditLogs { get; }
        DbSet<FileMetadata> FileMetadatas { get; }
        DbSet<FileUploadSession> FileUploadSessions { get; }
        DbSet<AlbumImage> AlbumImages { get; }
        DbSet<Artist> Artists { get; }
        DbSet<Album> Albums { get; }
        DbSet<Track> Tracks { get; }
        DbSet<Playlist> Playlists { get; }
        DbSet<PlaylistTrack> PlaylistTracks { get; }
        DbSet<ListenHistory> ListenHistories { get; }

        DatabaseFacade Database { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
