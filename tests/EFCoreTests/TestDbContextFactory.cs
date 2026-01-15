using Microsoft.EntityFrameworkCore;
using MusicService.Infrastructure.Persistence;

namespace Tests.EFCoreTests
{
    public static class TestDbContextFactory
    {
        public static MusicServiceDbContext Create(string databaseName)
        {
            var options = new DbContextOptionsBuilder<MusicServiceDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new MusicServiceDbContext(options);
        }
    }
}
