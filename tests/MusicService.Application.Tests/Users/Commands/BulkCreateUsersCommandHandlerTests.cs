using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Users.Commands;
using MusicService.Application.Users.Dtos;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Users.Commands;

public class BulkCreateUsersCommandHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<BulkCreateUsersCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldCreateUsersAndReturnSuccessItems()
    {
        var command = new BulkCreateUsersCommand
        {
            Commands = new List<CreateUserCommand> { new() { Username = "u", Email = "e@test.com", Password = "p" } }
        };
        _passwordHasher.Setup(h => h.HashPassword("p")).Returns("hashed");
        _userRepository.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => { u.Id = Guid.NewGuid(); return u; });
        var handler = new BulkCreateUsersCommandHandler(_userRepository.Object, _passwordHasher.Object, _mapper, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.SuccessfulCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Success);
    }

    [Fact]
    public async Task Handle_ShouldMarkDuplicateEmailsAsFailed()
    {
        var command = new BulkCreateUsersCommand
        {
            Commands = new List<CreateUserCommand> { new() { Username = "u", Email = "dup@test.com", Password = "p" } }
        };
        _userRepository.Setup(r => r.ExistsByEmailAsync("dup@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new BulkCreateUsersCommandHandler(_userRepository.Object, _passwordHasher.Object, _mapper, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.SuccessfulCount.Should().Be(0);
        result.Items.Should().ContainSingle(i => !i.Success && i.Error == "Email already in use");
    }
}
