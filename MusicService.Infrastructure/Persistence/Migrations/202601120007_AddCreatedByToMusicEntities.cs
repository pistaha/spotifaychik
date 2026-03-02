using FluentMigrator;
using System;

namespace MusicService.Infrastructure.Persistence.Migrations
{
    [Migration(202601120007)]
    public class AddCreatedByToMusicEntities : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("artists").Column("CreatedById").Exists())
            {
                Alter.Table("artists")
                    .AddColumn("CreatedById").AsGuid().Nullable().ForeignKey("users", "Id");
                Create.Index("IX_artists_createdby").OnTable("artists").OnColumn("CreatedById");
            }

            if (!Schema.Table("albums").Column("CreatedById").Exists())
            {
                Alter.Table("albums")
                    .AddColumn("CreatedById").AsGuid().Nullable().ForeignKey("users", "Id");
                Create.Index("IX_albums_createdby").OnTable("albums").OnColumn("CreatedById");
            }

            if (!Schema.Table("tracks").Column("CreatedById").Exists())
            {
                Alter.Table("tracks")
                    .AddColumn("CreatedById").AsGuid().Nullable().ForeignKey("users", "Id");
                Create.Index("IX_tracks_createdby").OnTable("tracks").OnColumn("CreatedById");
            }
        }

        public override void Down()
        {
            if (Schema.Table("tracks").Column("CreatedById").Exists())
            {
                Delete.Index("IX_tracks_createdby").OnTable("tracks");
                Delete.Column("CreatedById").FromTable("tracks");
            }

            if (Schema.Table("albums").Column("CreatedById").Exists())
            {
                Delete.Index("IX_albums_createdby").OnTable("albums");
                Delete.Column("CreatedById").FromTable("albums");
            }

            if (Schema.Table("artists").Column("CreatedById").Exists())
            {
                Delete.Index("IX_artists_createdby").OnTable("artists");
                Delete.Column("CreatedById").FromTable("artists");
            }
        }
    }
}
