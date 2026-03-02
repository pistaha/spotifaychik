using FluentMigrator;
using System;

namespace MusicService.Infrastructure.Persistence.Migrations
{
    [Migration(202601120008)]
    public class FileStorage : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("file_metadata").Exists())
            {
                Create.Table("file_metadata")
                    .WithColumn("Id").AsGuid().PrimaryKey()
                    .WithColumn("FileName").AsString(255).NotNullable()
                    .WithColumn("OriginalFileName").AsString(255).NotNullable()
                    .WithColumn("ContentType").AsString(100).NotNullable()
                    .WithColumn("Size").AsInt64().NotNullable()
                    .WithColumn("UploadedBy").AsGuid().NotNullable().ForeignKey("users", "Id")
                    .WithColumn("UploadedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                    .WithColumn("Path").AsString(500).NotNullable()
                    .WithColumn("Hash").AsString(128).NotNullable()
                    .WithColumn("IsPublic").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("ExpiresAt").AsDateTime().Nullable()
                    .WithColumn("DownloadCount").AsInt32().NotNullable().WithDefaultValue(0)
                    .WithColumn("Width").AsInt32().Nullable()
                    .WithColumn("Height").AsInt32().Nullable()
                    .WithColumn("ThumbnailSmallPath").AsString(500).Nullable()
                    .WithColumn("ThumbnailMediumPath").AsString(500).Nullable();

                Create.Index("IX_file_metadata_uploadedby").OnTable("file_metadata").OnColumn("UploadedBy");
                Create.Index("IX_file_metadata_hash").OnTable("file_metadata").OnColumn("Hash");
            }

            if (!Schema.Table("file_upload_sessions").Exists())
            {
                Create.Table("file_upload_sessions")
                    .WithColumn("Id").AsGuid().PrimaryKey()
                    .WithColumn("UploadId").AsString(100).NotNullable().Unique()
                    .WithColumn("UploadedBy").AsGuid().NotNullable().ForeignKey("users", "Id")
                    .WithColumn("FileName").AsString(255).NotNullable()
                    .WithColumn("TotalChunks").AsInt32().NotNullable()
                    .WithColumn("UploadedChunks").AsInt32().NotNullable().WithDefaultValue(0)
                    .WithColumn("TotalSize").AsInt64().NotNullable().WithDefaultValue(0)
                    .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                    .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                    .WithColumn("IsCompleted").AsBoolean().NotNullable().WithDefaultValue(false);
            }

            if (!Schema.Table("album_images").Exists())
            {
                Create.Table("album_images")
                    .WithColumn("AlbumId").AsGuid().NotNullable().ForeignKey("albums", "Id")
                    .WithColumn("FileId").AsGuid().NotNullable().ForeignKey("file_metadata", "Id")
                    .WithColumn("IsMain").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("Order").AsInt32().NotNullable().WithDefaultValue(0)
                    .WithColumn("AttachedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

                Create.PrimaryKey("PK_album_images").OnTable("album_images").Columns("AlbumId", "FileId");
            }
        }

        public override void Down()
        {
            if (Schema.Table("album_images").Exists())
            {
                Delete.Table("album_images");
            }

            if (Schema.Table("file_upload_sessions").Exists())
            {
                Delete.Table("file_upload_sessions");
            }

            if (Schema.Table("file_metadata").Exists())
            {
                Delete.Table("file_metadata");
            }
        }
    }
}
