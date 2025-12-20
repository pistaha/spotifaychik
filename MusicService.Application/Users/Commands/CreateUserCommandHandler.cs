using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Users.Dtos;
using MusicService.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Users.Commands
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IMapper mapper,
            ILogger<CreateUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating user: {Username}", request.Username);

            // Проверка уникальности
            var existingUser = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
            if (existingUser != null)
                throw new ArgumentException($"User with email {request.Email} already exists");

            existingUser = await _userRepository.FindByUsernameAsync(request.Username, cancellationToken);
            if (existingUser != null)
                throw new ArgumentException($"User with username {request.Username} already exists");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                DisplayName = request.DisplayName,
                DateOfBirth = request.DateOfBirth,
                Country = request.Country,
                FavoriteGenres = request.FavoriteGenres,
                LastLogin = DateTime.UtcNow,
                ListenTimeMinutes = 0
            };

            var createdUser = await _userRepository.CreateAsync(user, cancellationToken);
            
            _logger.LogInformation("User {UserId} created successfully", createdUser.Id);
            
            return _mapper.Map<UserDto>(createdUser);
        }
    }
}
