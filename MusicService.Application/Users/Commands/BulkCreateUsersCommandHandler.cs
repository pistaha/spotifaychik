using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Users.Dtos;
using MusicService.Domain.Entities;

namespace MusicService.Application.Users.Commands
{
    public class BulkCreateUsersCommandHandler : IRequestHandler<BulkCreateUsersCommand, BulkOperationResult<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ILogger<BulkCreateUsersCommandHandler> _logger;

        public BulkCreateUsersCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IMapper mapper,
            ILogger<BulkCreateUsersCommandHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BulkOperationResult<UserDto>> Handle(BulkCreateUsersCommand request, CancellationToken cancellationToken)
        {
            var result = new BulkOperationResult<UserDto>
            {
                TotalCount = request.Commands.Count
            };

            foreach (var command in request.Commands)
            {
                try
                {
                    if (await _userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
                    {
                        result.Items.Add(new BulkOperationItem<UserDto>
                        {
                            Success = false,
                            Message = $"User with email {command.Email} already exists",
                            Error = "Email already in use"
                        });
                        continue;
                    }

                    if (await _userRepository.ExistsByUsernameAsync(command.Username, cancellationToken))
                    {
                        result.Items.Add(new BulkOperationItem<UserDto>
                        {
                            Success = false,
                            Message = $"User with username {command.Username} already exists",
                            Error = "Username already in use"
                        });
                        continue;
                    }

                    var user = new User
                    {
                        Username = command.Username,
                        Email = command.Email,
                        PasswordHash = _passwordHasher.HashPassword(command.Password),
                        DisplayName = command.DisplayName,
                        DateOfBirth = command.DateOfBirth,
                        Country = command.Country,
                        FavoriteGenres = new List<string>(command.FavoriteGenres),
                        LastLogin = DateTime.UtcNow,
                        ListenTimeMinutes = 0
                    };

                    var createdUser = await _userRepository.CreateAsync(user, cancellationToken);

                    result.Items.Add(new BulkOperationItem<UserDto>
                    {
                        Success = true,
                        Message = $"User {createdUser.Username} created successfully",
                        Data = _mapper.Map<UserDto>(createdUser),
                        ItemId = createdUser.Id
                    });

                    result.SuccessfulCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create user {Username} during bulk operation", command.Username);
                    result.Items.Add(new BulkOperationItem<UserDto>
                    {
                        Success = false,
                        Message = $"Error creating user {command.Username}",
                        Error = ex.Message
                    });
                }
            }

            result.FailedCount = result.TotalCount - result.SuccessfulCount;
            return result;
        }
    }
}
