using FluentMigrator;

namespace MusicService.Infrastructure.Persistence.Migrations
{
    [Migration(202601120006)]
    public class AddEmailConfirmationToken : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("users").Column("EmailConfirmationToken").Exists())
            {
                Alter.Table("users")
                    .AddColumn("EmailConfirmationToken").AsString(200).NotNullable().WithDefaultValue(string.Empty);
            }
        }

        public override void Down()
        {
            if (Schema.Table("users").Column("EmailConfirmationToken").Exists())
            {
                Delete.Column("EmailConfirmationToken").FromTable("users");
            }
        }
    }
}
