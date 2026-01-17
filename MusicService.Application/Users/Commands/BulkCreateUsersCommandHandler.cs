using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common;
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

            var initialFailures = new List<BulkOperationItem<UserDto>>();
            var commandsToProcess = new List<CreateUserCommand>();
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var command in request.Commands)
            {
                var emailKey = command.Email ?? string.Empty;
                var usernameKey = command.Username ?? string.Empty;

                if (!seenEmails.Add(emailKey))
                {
                    initialFailures.Add(new BulkOperationItem<UserDto>
                    {
                        Success = false,
                        Message = $"User with email {command.Email} already exists",
                        Error = "Email already in use"
                    });
                    continue;
                }

                if (!seenUsernames.Add(usernameKey))
                {
                    initialFailures.Add(new BulkOperationItem<UserDto>
                    {
                        Success = false,
                        Message = $"User with username {command.Username} already exists",
                        Error = "Username already in use"
                    });
                    continue;
                }

                commandsToProcess.Add(command);
            }

            var maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                IDbContextTransaction? transaction = null;
                var attemptItems = new List<BulkOperationItem<UserDto>>(initialFailures);
                var successfulCount = 0;
                var processedCommands = 0;
                DbContext? efContext = _dbContext as DbContext;
                try
                {
                    var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
                    var isPostgres = _dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
                    if (efContext != null)
                    {
                        efContext.ChangeTracker.Clear();
                    }
                    if (!isInMemory)
                    {
                        transaction = await _dbContext.Database.BeginTransactionAsync(
                            IsolationLevel.Serializable, cancellationToken);
                    }

                    var supportsSavepoints = transaction?.SupportsSavepoints == true;

                    var itemIndex = 0;
                    foreach (var command in commandsToProcess)
                    {
                        processedCommands++;
                        var savepointName = $"user_{itemIndex++}";
                        if (supportsSavepoints && transaction != null)
                        {
                            await transaction.CreateSavepointAsync(savepointName, cancellationToken);
                        }

                        var now = DateTime.UtcNow;
                        var genres = command.FavoriteGenres ?? new List<string>();
                        var displayName = string.IsNullOrWhiteSpace(command.DisplayName)
                            ? $"{command.FirstName} {command.LastName}".Trim()
                            : command.DisplayName;
                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = command.Username;
                        }

                        var hash = _passwordHasher.HashPassword(command.Password, out var salt);

                        var user = new User
                        {
                            Id = Guid.NewGuid(),
                            Username = command.Username,
                            Email = command.Email,
                            PasswordHash = hash,
                            PasswordSalt = salt,
                            FirstName = command.FirstName,
                            LastName = command.LastName,
                            DisplayName = displayName,
                            DateOfBirth = command.DateOfBirth,
                            Country = command.Country,
                            PhoneNumber = command.PhoneNumber,
                            FavoriteGenres = new List<string>(genres),
                            LastLoginAt = now,
                            ListenTimeMinutes = 0,
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsActive = true,
                            IsEmailConfirmed = false,
                            IsDeleted = false
                        };

                        try
                        {
                            if (isPostgres)
                            {
                                var rows = await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO users (""Id"", ""Username"", ""Email"", ""PasswordHash"", ""PasswordSalt"", ""FirstName"", ""LastName"", ""DisplayName"", ""ProfileImage"", ""DateOfBirth"", ""PhoneNumber"", ""Country"", ""FavoriteGenres"", ""ListenTimeMinutes"", ""LastLoginAt"", ""IsEmailConfirmed"", ""IsActive"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"")
VALUES ({user.Id}, {user.Username}, {user.Email}, {user.PasswordHash}, {user.PasswordSalt}, {user.FirstName}, {user.LastName}, {user.DisplayName}, {user.ProfileImage}, {user.DateOfBirth}, {user.PhoneNumber}, {user.Country}, {genres.ToArray()}, {user.ListenTimeMinutes}, {user.LastLoginAt}, {user.IsEmailConfirmed}, {user.IsActive}, {user.IsDeleted}, {user.CreatedAt}, {user.UpdatedAt})
ON CONFLICT DO NOTHING;");
                                if (rows == 0)
                                {
                                    if (supportsSavepoints && transaction != null)
                                    {
                                        await transaction.ReleaseSavepointAsync(savepointName, cancellationToken);
                                    }
                                    attemptItems.Add(new BulkOperationItem<UserDto>
                                    {
                                        Success = false,
                                        Message = $"User {command.Username} already exists",
                                        Error = "Email or username already in use"
                                    });
                                    continue;
                                }
                            }
                            else
                            {
                                if (efContext != null)
                                {
                                    efContext.ChangeTracker.Clear();
                                }
                                _dbContext.Users.Add(user);
                                await _dbContext.SaveChangesAsync(cancellationToken);
                                if (efContext != null)
                                {
                                    efContext.ChangeTracker.Clear();
                                }
                            }

                            if (supportsSavepoints && transaction != null)
                            {
                                await transaction.ReleaseSavepointAsync(savepointName, cancellationToken);
                            }

                            attemptItems.Add(new BulkOperationItem<UserDto>
                            {
                                Success = true,
                                Message = $"User {user.Username} created successfully",
                                Data = _mapper.Map<UserDto>(user),
                                ItemId = user.Id
                            });
                            successfulCount++;
                        }
                        catch (Exception ex)
                        {
                            if (supportsSavepoints && transaction != null)
                            {
                                await transaction.RollbackToSavepointAsync(savepointName, cancellationToken);
                            }

                            if (DatabaseErrorDetector.IsTransient(ex))
                            {
                                throw;
                            }

                            if (!isPostgres && _dbContext is DbContext context)
                            {
                                context.ChangeTracker.Clear();
                            }

                            attemptItems.Add(new BulkOperationItem<UserDto>
                            {
                                Success = false,
                                Message = $"Error creating user {command.Username}",
                                Error = DatabaseErrorDetector.IsUniqueViolation(ex)
                                    ? "Email or username already in use"
                                    : ex.Message
                            });
                        }
                    }

                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }

                    result.Items = attemptItems;
                    result.SuccessfulCount = successfulCount;
                    result.FailedCount = result.TotalCount - result.SuccessfulCount;
                    return result;
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    if (DatabaseErrorDetector.IsTransient(ex) && attempt < maxAttempts)
                    {
                        efContext?.ChangeTracker.Clear();
                        await DelayAsync(attempt, cancellationToken);
                        continue;
                    }

                    _logger.LogError(ex, "Failed to save users during bulk operation");
                    for (var index = processedCommands; index < commandsToProcess.Count; index++)
                    {
                        var command = commandsToProcess[index];
                        attemptItems.Add(new BulkOperationItem<UserDto>
                        {
                            Success = false,
                            Message = $"Error creating user {command.Username}",
                            Error = ex.Message
                        });
                    }

                    result.Items = attemptItems;
                    result.SuccessfulCount = successfulCount;
                    result.FailedCount = result.TotalCount - result.SuccessfulCount;
                    return result;
                }
                finally
                {
                    if (transaction != null)
                    {
                        await transaction.DisposeAsync();
                    }
                }
            }

            return result;
        }

        private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
        {
            var delayMs = 50 * (int)Math.Pow(2, attempt - 1);
            return Task.Delay(delayMs, cancellationToken);
        }

    }
}
