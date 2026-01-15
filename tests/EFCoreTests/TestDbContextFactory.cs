using Microsoft.EntityFrameworkCore;
using MusicService.Infrastructure.Persistence;

namespace Tests.EFCoreTests
{
    public static class TestDbContextFactory
    {
        public static MusicServiceDbContext Create(string databaseName)
        {
            var options = new DbContextOptionsBuilder<MusicServiceDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            var context = new MusicServiceDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            return context;
        }
    }
}
