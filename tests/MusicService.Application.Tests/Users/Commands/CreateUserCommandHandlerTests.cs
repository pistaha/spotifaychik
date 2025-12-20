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

public class CreateUserCommandHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<CreateUserCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ShouldHashPasswordAndReturnDto()
    {
        var command = new CreateUserCommand { Username = "user", Email = "email@test.com", Password = "pass" };
        _passwordHasher.Setup(h => h.HashPassword(command.Password)).Returns("hashed");
        _userRepository.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => { u.Id = Guid.NewGuid(); return u; });
        var handler = new CreateUserCommandHandler(_userRepository.Object, _passwordHasher.Object, _mapper, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeOfType<UserDto>();
        _passwordHasher.Verify(h => h.HashPassword("pass"), Times.Once);
        result.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEmailExists()
    {
        var command = new CreateUserCommand { Username = "user", Email = "email@test.com", Password = "pass" };
        _userRepository.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User());
        var handler = new CreateUserCommandHandler(_userRepository.Object, _passwordHasher.Object, _mapper, _logger.Object);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
