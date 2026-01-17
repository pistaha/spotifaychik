using FluentMigrator;
using System;

namespace MusicService.Infrastructure.Persistence.Migrations
{
    [Migration(202601120005)]
    public class PermissionsAndAudit : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("permissions").Exists())
            {
                Create.Table("permissions")
                    .WithColumn("Id").AsGuid().PrimaryKey()
                    .WithColumn("Name").AsString(100).NotNullable().Unique()
                    .WithColumn("Description").AsString(200).Nullable()
                    .WithColumn("Category").AsString(100).NotNullable()
                    .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                    .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);
            }

            if (!Schema.Table("role_permissions").Exists())
            {
                Create.Table("role_permissions")
                    .WithColumn("RoleId").AsGuid().NotNullable().ForeignKey("roles", "Id")
                    .WithColumn("PermissionId").AsGuid().NotNullable().ForeignKey("permissions", "Id");

                Create.PrimaryKey("PK_role_permissions").OnTable("role_permissions").Columns("RoleId", "PermissionId");
            }

            if (!Schema.Table("security_audit_logs").Exists())
            {
                Create.Table("security_audit_logs")
                    .WithColumn("Id").AsGuid().PrimaryKey()
                    .WithColumn("EventType").AsInt32().NotNullable()
                    .WithColumn("UserId").AsGuid().Nullable()
                    .WithColumn("Email").AsString(200).Nullable()
                    .WithColumn("IpAddress").AsString(100).Nullable()
                    .WithColumn("UserAgent").AsString(300).Nullable()
                    .WithColumn("Success").AsBoolean().NotNullable()
                    .WithColumn("Details").AsString(2000).Nullable()
                    .WithColumn("Timestamp").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

                Create.Index("IX_security_audit_logs_userid").OnTable("security_audit_logs").OnColumn("UserId");
                Create.Index("IX_security_audit_logs_eventtype").OnTable("security_audit_logs").OnColumn("EventType");
            }

            var adminRoleId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
            var userRoleId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");
            var moderatorRoleId = Guid.Parse("cccccccc-1111-1111-1111-111111111111");

            var permissions = new[]
            {
                new { Id = Guid.Parse("11111111-2222-2222-2222-222222222222"), Name = "CanDeleteTracks", Description = "Delete tracks", Category = "Tracks" },
                new { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "CanEditMetadata", Description = "Edit metadata", Category = "Metadata" },
                new { Id = Guid.Parse("33333333-2222-2222-2222-222222222222"), Name = "CanViewAuditLogs", Description = "View audit logs", Category = "Security" },
                new { Id = Guid.Parse("44444444-2222-2222-2222-222222222222"), Name = "CanManageUsers", Description = "Manage users", Category = "Users" },
                new { Id = Guid.Parse("55555555-2222-2222-2222-222222222222"), Name = "CanModerateContent", Description = "Moderate content", Category = "Content" }
            };

            foreach (var permission in permissions)
            {
                Execute.Sql($"""
                    INSERT INTO permissions ("Id", "Name", "Description", "Category", "CreatedAt", "UpdatedAt")
                    VALUES ('{permission.Id}', '{permission.Name}', '{permission.Description}', '{permission.Category}', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                    ON CONFLICT ("Id") DO NOTHING;
                    """);
            }

            void Grant(Guid roleId, Guid permissionId)
            {
                Execute.Sql($"""
                    INSERT INTO role_permissions ("RoleId", "PermissionId")
                    VALUES ('{roleId}', '{permissionId}')
                    ON CONFLICT ("RoleId", "PermissionId") DO NOTHING;
                    """);
            }

            foreach (var permission in permissions)
            {
                Grant(adminRoleId, permission.Id);
            }

            Grant(moderatorRoleId, Guid.Parse("11111111-2222-2222-2222-222222222222"));
            Grant(moderatorRoleId, Guid.Parse("22222222-2222-2222-2222-222222222222"));
            Grant(moderatorRoleId, Guid.Parse("55555555-2222-2222-2222-222222222222"));

            Grant(userRoleId, Guid.Parse("55555555-2222-2222-2222-222222222222"));
        }

        public override void Down()
        {
            if (Schema.Table("security_audit_logs").Exists())
            {
                Delete.Table("security_audit_logs");
            }

            if (Schema.Table("role_permissions").Exists())
            {
                Delete.Table("role_permissions");
            }

            if (Schema.Table("permissions").Exists())
            {
                Delete.Table("permissions");
            }
        }
    }
}
