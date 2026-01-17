using FluentMigrator;
using System;

namespace MusicService.Infrastructure.Persistence.Migrations
{
    [Migration(202601120002)]
    public class AuthAndSecurity : Migration
    {
        public override void Up()
        {
            Alter.Table("users")
                .AddColumn("PasswordSalt").AsString(200).NotNullable().WithDefaultValue(string.Empty)
                .AddColumn("FirstName").AsString(100).Nullable()
                .AddColumn("LastName").AsString(100).Nullable()
                .AddColumn("PhoneNumber").AsString(30).Nullable()
                .AddColumn("LastLoginAt").AsDateTime().Nullable()
                .AddColumn("IsEmailConfirmed").AsBoolean().NotNullable().WithDefaultValue(false)
                .AddColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .AddColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);

            Alter.Table("users")
                .AlterColumn("Email").AsString(200).NotNullable()
                .AlterColumn("DisplayName").AsString(150).NotNullable();

            Alter.Table("user_roles")
                .AddColumn("AssignedBy").AsGuid().Nullable();

            Create.Table("user_sessions")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("UserId").AsGuid().NotNullable().ForeignKey("users", "Id")
                .WithColumn("RefreshTokenHash").AsString(500).NotNullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("ExpiresAt").AsDateTime().NotNullable()
                .WithColumn("DeviceInfo").AsString(200).Nullable()
                .WithColumn("IpAddress").AsString(100).Nullable()
                .WithColumn("IsRevoked").AsBoolean().NotNullable().WithDefaultValue(false);

            Create.Index("IX_user_sessions_userid").OnTable("user_sessions").OnColumn("UserId");

            Create.Table("user_claims")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("UserId").AsGuid().NotNullable().ForeignKey("users", "Id")
                .WithColumn("ClaimType").AsString(100).NotNullable()
                .WithColumn("ClaimValue").AsString(500).NotNullable();

            Create.Index("IX_user_claims_userid_type").OnTable("user_claims")
                .OnColumn("UserId").Ascending()
                .OnColumn("ClaimType").Ascending();


            var adminRoleId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
            var userRoleId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");
            var moderatorRoleId = Guid.Parse("cccccccc-1111-1111-1111-111111111111");
            Insert.IntoTable("roles").Row(new
            {
                Id = adminRoleId,
                Name = "Admin",
                Description = "System administrator",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            Insert.IntoTable("roles").Row(new
            {
                Id = userRoleId,
                Name = "User",
                Description = "Default user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            Insert.IntoTable("roles").Row(new
            {
                Id = moderatorRoleId,
                Name = "Moderator",
                Description = "Content moderator",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public override void Down()
        {
            Delete.Table("user_claims");
            Delete.Table("user_sessions");

            Delete.Column("AssignedBy").FromTable("user_roles");

            Delete.Column("PasswordSalt").FromTable("users");
            Delete.Column("FirstName").FromTable("users");
            Delete.Column("LastName").FromTable("users");
            Delete.Column("PhoneNumber").FromTable("users");
            Delete.Column("LastLoginAt").FromTable("users");
            Delete.Column("IsEmailConfirmed").FromTable("users");
            Delete.Column("IsActive").FromTable("users");
            Delete.Column("IsDeleted").FromTable("users");
        }
    }
}
