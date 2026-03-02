using FluentMigrator;

namespace MusicService.Infrastructure.Persistence.Migrations
{
    [Migration(202601120003)]
    public class RemoveLegacyUserFields : Migration
    {
        public override void Up()
        {
            if (Schema.Table("users").Column("LastLogin").Exists())
            {
                Delete.Column("LastLogin").FromTable("users");
            }
        }

        public override void Down()
        {
            if (!Schema.Table("users").Column("LastLogin").Exists())
            {
                Alter.Table("users")
                    .AddColumn("LastLogin").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);
            }
        }
    }
}
