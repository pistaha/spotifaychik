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
