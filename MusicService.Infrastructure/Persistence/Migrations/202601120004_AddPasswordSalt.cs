using FluentMigrator;

namespace MusicService.Infrastructure.Persistence.Migrations
{
    [Migration(202601120004)]
    public class AddPasswordSalt : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("users").Column("PasswordSalt").Exists())
            {
                Alter.Table("users")
                    .AddColumn("PasswordSalt").AsString(200).NotNullable().WithDefaultValue(string.Empty);
            }
        }

        public override void Down()
        {
            if (Schema.Table("users").Column("PasswordSalt").Exists())
            {
                Delete.Column("PasswordSalt").FromTable("users");
            }
        }
    }
}
