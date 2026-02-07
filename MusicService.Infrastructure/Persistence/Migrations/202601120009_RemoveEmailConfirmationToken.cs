using FluentMigrator;

namespace MusicService.Infrastructure.Persistence.Migrations
{
    [Migration(202601120009)]
    public class RemoveEmailConfirmationToken : Migration
    {
        public override void Up()
        {
            if (Schema.Table("users").Column("EmailConfirmationToken").Exists())
            {
                Delete.Column("EmailConfirmationToken").FromTable("users");
            }
        }

        public override void Down()
        {
            if (!Schema.Table("users").Column("EmailConfirmationToken").Exists())
            {
                Alter.Table("users")
                    .AddColumn("EmailConfirmationToken").AsString(200).NotNullable().WithDefaultValue(string.Empty);
            }
        }
    }
}
