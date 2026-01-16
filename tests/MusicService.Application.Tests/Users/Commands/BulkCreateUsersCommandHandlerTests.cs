using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Users.Commands;
using Microsoft.Extensions.Logging;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Users.Commands;

public class BulkCreateUsersCommandHandlerTests
{
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    [Fact]
    public async Task Handle_ShouldCreateUsersAndReturnSuccessItems()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var command = new BulkCreateUsersCommand
        {
            Commands = new List<CreateUserCommand> { new() { Username = "u", Email = "e@test.com", Password = "p" } }
        };
        _passwordHasher.Setup(h => h.HashPassword("p")).Returns("hashed");
        var handler = new BulkCreateUsersCommandHandler(
            dbContext,
            _passwordHasher.Object,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<BulkCreateUsersCommandHandler>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.SuccessfulCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Success);
    }

    [Fact]
    public async Task Handle_ShouldMarkDuplicateEmailsAsFailed()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var command = new BulkCreateUsersCommand
        {
            Commands = new List<CreateUserCommand>
            {
                new() { Username = "u1", Email = "dup@test.com", Password = "p" },
                new() { Username = "u2", Email = "dup@test.com", Password = "p" }
            }
        };
        var handler = new BulkCreateUsersCommandHandler(
            dbContext,
            _passwordHasher.Object,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<BulkCreateUsersCommandHandler>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Items.Should().ContainSingle(i => !i.Success && i.Error == "Email already in use");
    }
}
