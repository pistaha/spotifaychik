using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Configuration;
using MusicService.Infrastructure.Repositories;
using Tests.TestUtilities;
using Xunit;

namespace Tests.MusicService.Infrastructure.Tests.Repositories;

public class FileStorageRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistEntityAndAssignMetadata()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath);

        var created = await repository.CreateAsync(new TestEntity { Name = "Lo-Fi Study Mix", Value = 42 });

        created.Id.Should().NotBeEmpty();
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(3));
        created.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(3));

        var fetched = await repository.GetByIdAsync(created.Id);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Lo-Fi Study Mix");
        fetched.Value.Should().Be(42);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceEntity_WhenItExists()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath);
        var created = await repository.CreateAsync(new TestEntity { Name = "Focus Mix", Value = 10 });

        created.Value = 25;

        var updated = await repository.UpdateAsync(created);

        updated.Should().NotBeNull();
        updated!.Value.Should().Be(25);

        var fetched = await repository.GetByIdAsync(created.Id);
        fetched!.Value.Should().Be(25);
        fetched.UpdatedAt.Should().BeAfter(fetched.CreatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity_AndReturnFalseForMissing()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath);
        var created = await repository.CreateAsync(new TestEntity { Name = "Delete Me", Value = 5 });

        var removed = await repository.DeleteAsync(created.Id);
        var removedAgain = await repository.DeleteAsync(created.Id);

        removed.Should().BeTrue();
        removedAgain.Should().BeFalse();

        var all = await repository.GetAllAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAsync_ShouldApplyComplexPredicate()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath);
        await repository.SeedAsync(new[]
        {
            new TestEntity { Name = "Morning Focus Mix", Value = 5 },
            new TestEntity { Name = "Night Coding Mix", Value = 15 },
            new TestEntity { Name = "Random Playlist", Value = 30 }
        });

        var matches = await repository.FindAsync(entity =>
            entity.Name.Contains("Mix", StringComparison.OrdinalIgnoreCase) && entity.Value > 10);

        matches.Should().HaveCount(1);
        matches.Single().Name.Should().Be("Night Coding Mix");
    }

    [Fact]
    public async Task CreateAsync_ShouldRemainThreadSafe_WhenCalledConcurrently()
    {
        using var storage = new TempFileStorage();
        var repository = CreateRepository(storage.FilePath);

        var tasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => repository.CreateAsync(new TestEntity
            {
                Name = $"Entity-{i}",
                Value = i
            })));

        await Task.WhenAll(tasks);
        var all = await repository.GetAllAsync();

        all.Should().HaveCount(50);
        all.Select(x => x.Name).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetAllAsync_ShouldThrowInvalidOperationException_WhenJsonCorrupted()
    {
        using var storage = new TempFileStorage();
        await File.WriteAllTextAsync(storage.FilePath, "not-a-json-array");

        var repository = CreateRepository(storage.FilePath);

        var act = async () => await repository.GetAllAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid JSON format*");
    }

    private static TestEntityRepository CreateRepository(string filePath)
    {
        var logger = Mock.Of<ILogger<TestEntityRepository>>();
        var options = Options.Create(new FileStorageOptions
        {
            PrettyPrintJson = false,
            Backup = new BackupOptions
            {
                Enabled = false
            }
        });

        return new TestEntityRepository(filePath, logger, options);
    }

    public sealed class TestEntityRepository : FileStorageRepository<TestEntity>
    {
        public TestEntityRepository(string filePath, ILogger<TestEntityRepository> logger, IOptions<FileStorageOptions> options)
            : base(filePath, logger, options)
        {
        }

        public Task SeedAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.Select(entity =>
            {
                if (entity.Id == Guid.Empty)
                {
                    entity.Id = Guid.NewGuid();
                }

                if (entity.CreatedAt == default)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }

                if (entity.UpdatedAt == default)
                {
                    entity.UpdatedAt = entity.CreatedAt;
                }

                return entity;
            }).ToList();

            return WriteAllAsync(list, cancellationToken);
        }
    }

    public sealed class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
