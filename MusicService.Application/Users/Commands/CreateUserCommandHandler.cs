using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Users.Dtos;
using MusicService.Domain.Entities;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Users.Commands
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IMusicServiceDbContext dbContext,
            IPasswordHasher passwordHasher,
            IMapper mapper,
            ILogger<CreateUserCommandHandler> logger)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating user: {Username}", request.Username);

            var maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                IDbContextTransaction? transaction = null;
                try
                {
                    var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
                    if (!isInMemory)
                    {
                        transaction = await _dbContext.Database.BeginTransactionAsync(
                            IsolationLevel.Serializable, cancellationToken);
                    }

                    var emailExists = await _dbContext.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Email == request.Email, cancellationToken);
                    if (emailExists)
                        throw new ArgumentException($"User with email {request.Email} already exists");

                    var usernameExists = await _dbContext.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Username == request.Username, cancellationToken);
                    if (usernameExists)
                        throw new ArgumentException($"User with username {request.Username} already exists");

                    var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
                        ? $"{request.FirstName} {request.LastName}".Trim()
                        : request.DisplayName;
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = request.Username;
                    }

                    var hash = _passwordHasher.HashPassword(request.Password, out var salt);

                    var user = new User
                    {
                        Username = request.Username,
                        Email = request.Email,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        DisplayName = displayName,
                        DateOfBirth = request.DateOfBirth,
                        Country = request.Country,
                        PhoneNumber = request.PhoneNumber,
                        FavoriteGenres = request.FavoriteGenres,
                        LastLoginAt = null,
                        ListenTimeMinutes = 0,
                        IsActive = true,
                        IsEmailConfirmed = false,
                        IsDeleted = false
                    };

                    var userRoleId = await _dbContext.Roles
                        .AsNoTracking()
                        .Where(r => r.Name == "User")
                        .Select(r => (Guid?)r.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (userRoleId.HasValue)
                    {
                        _dbContext.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = userRoleId.Value,
                            AssignedAt = DateTime.UtcNow
                        });
                    }

                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }

                    _logger.LogInformation("User {UserId} created successfully", user.Id);

                    return _mapper.Map<UserDto>(user);
                }
                catch (DbUpdateException ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    if (DatabaseErrorDetector.IsUniqueViolation(ex))
                    {
                        _logger.LogWarning(ex, "Unique constraint violation while creating user {Username}", request.Username);
                        throw new ArgumentException("User with the same email or username already exists");
                    }

                    if (DatabaseErrorDetector.IsTransient(ex) && attempt < maxAttempts)
                    {
                        await DelayAsync(attempt, cancellationToken);
                        continue;
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    if (DatabaseErrorDetector.IsTransient(ex) && attempt < maxAttempts)
                    {
                        await DelayAsync(attempt, cancellationToken);
                        continue;
                    }

                    throw;
                }
                finally
                {
                    if (transaction != null)
                    {
                        await transaction.DisposeAsync();
                    }
                }
            }

            throw new InvalidOperationException("Failed to create user after multiple attempts.");
        }

        private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
        {
            var delayMs = 50 * (int)Math.Pow(2, attempt - 1);
            return Task.Delay(delayMs, cancellationToken);
        }
    }
}
