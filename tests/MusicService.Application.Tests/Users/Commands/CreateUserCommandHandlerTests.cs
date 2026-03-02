using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Users.Commands;
using MusicService.Application.Users.Dtos;
using MusicService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Users.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    [Fact]
    public async Task Handle_ShouldHashPasswordAndReturnDto()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var command = new CreateUserCommand { Username = "user", Email = "email@test.com", Password = "pass" };
        var salt = "salt";
        _passwordHasher.Setup(h => h.HashPassword(command.Password, out salt)).Returns("hashed");
        var handler = new CreateUserCommandHandler(
            dbContext,
            _passwordHasher.Object,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<CreateUserCommandHandler>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeOfType<UserDto>();
        _passwordHasher.Verify(h => h.HashPassword("pass", out salt), Times.Once);
        result.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEmailExists()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var command = new CreateUserCommand { Username = "user", Email = "email@test.com", Password = "pass" };
        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "existing",
            Email = command.Email,
            PasswordHash = "hash",
            DisplayName = "Existing",
            Country = "US",
            FavoriteGenres = new List<string>()
        });
        await dbContext.SaveChangesAsync();
        var handler = new CreateUserCommandHandler(
            dbContext,
            _passwordHasher.Object,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<CreateUserCommandHandler>());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
