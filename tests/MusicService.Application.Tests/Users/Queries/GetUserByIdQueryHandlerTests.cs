using AutoMapper;
using FluentAssertions;
using Moq;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Users.Queries;
using MusicService.Domain.Entities;
using Xunit;

namespace Tests.MusicService.Application.Tests.Users.Queries;

public class GetUserByIdQueryHandlerTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly Mock<IUserRepository> _userRepository = new();

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenUserMissing()
    {
        var handler = new GetUserByIdQueryHandler(_userRepository.Object, _mapper);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await handler.Handle(new GetUserByIdQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapUser_WhenFound()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "user" };
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = new GetUserByIdQueryHandler(_userRepository.Object, _mapper);

        var result = await handler.Handle(new GetUserByIdQuery { UserId = user.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }
}
