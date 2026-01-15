using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Users.Dtos;
using MusicService.Domain.Entities;

namespace MusicService.Application.Users.Commands
{
    public class BulkCreateUsersCommandHandler : IRequestHandler<BulkCreateUsersCommand, BulkOperationResult<UserDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ILogger<BulkCreateUsersCommandHandler> _logger;

        public BulkCreateUsersCommandHandler(
            IMusicServiceDbContext dbContext,
            IPasswordHasher passwordHasher,
            IMapper mapper,
            ILogger<BulkCreateUsersCommandHandler> logger)
        {
            _dbContext = dbContext;
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

            var usersToCreate = new List<User>();

            foreach (var command in request.Commands)
            {
                try
                {
                    var emailExists = await _dbContext.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Email == command.Email, cancellationToken);
                    if (emailExists)
                    {
                        result.Items.Add(new BulkOperationItem<UserDto>
                        {
                            Success = false,
                            Message = $"User with email {command.Email} already exists",
                            Error = "Email already in use"
                        });
                        continue;
                    }

                    var usernameExists = await _dbContext.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Username == command.Username, cancellationToken);
                    if (usernameExists)
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

                    usersToCreate.Add(user);
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

            if (usersToCreate.Count > 0)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    _dbContext.Users.AddRange(usersToCreate);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    foreach (var user in usersToCreate)
                    {
                        result.Items.Add(new BulkOperationItem<UserDto>
                        {
                            Success = true,
                            Message = $"User {user.Username} created successfully",
                            Data = _mapper.Map<UserDto>(user),
                            ItemId = user.Id
                        });
                        result.SuccessfulCount++;
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Failed to save users during bulk operation");
                    foreach (var user in usersToCreate)
                    {
                        result.Items.Add(new BulkOperationItem<UserDto>
                        {
                            Success = false,
                            Message = $"Error creating user {user.Username}",
                            Error = ex.Message
                        });
                    }
                }
            }

            result.FailedCount = result.TotalCount - result.SuccessfulCount;
            return result;
        }
    }
}
