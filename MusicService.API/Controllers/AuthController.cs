using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicService.API.Authentication;
using MusicService.API.Models;
using MusicService.Application.Authentication.Dtos;
using MusicService.Application.Common;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Users.Dtos;
using MusicService.Domain.Entities;
using DomainUserClaim = MusicService.Domain.Entities.UserClaim;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string PasswordResetClaimType = "PasswordResetToken";
        private const string EmailConfirmationClaimType = "EmailConfirmationToken";
        private static readonly HashSet<string> ReservedClaimTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            PasswordResetClaimType,
            EmailConfirmationClaimType
        };

        private readonly IMusicServiceDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly ISecurityAuditService _auditService;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly IValidator<RefreshTokenDto> _refreshValidator;
        private readonly IValidator<RevokeTokenDto> _revokeValidator;
        private readonly IValidator<ForgotPasswordDto> _forgotValidator;
        private readonly IValidator<ResetPasswordDto> _resetValidator;
        private readonly IValidator<ChangePasswordDto> _changeValidator;

        public AuthController(
            IMusicServiceDbContext dbContext,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IOptions<JwtSettings> jwtOptions,
            ISecurityAuditService auditService,
            IValidator<RegisterDto> registerValidator,
            IValidator<LoginDto> loginValidator,
            IValidator<RefreshTokenDto> refreshValidator,
            IValidator<RevokeTokenDto> revokeValidator,
            IValidator<ForgotPasswordDto> forgotValidator,
            IValidator<ResetPasswordDto> resetValidator,
            IValidator<ChangePasswordDto> changeValidator)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _jwtSettings = jwtOptions.Value;
            _auditService = auditService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _refreshValidator = refreshValidator;
            _revokeValidator = revokeValidator;
            _forgotValidator = forgotValidator;
            _resetValidator = resetValidator;
            _changeValidator = changeValidator;
        }

        /// <summary>
        /// регистрирует нового пользователя.
        /// </summary>
        /// <param name="request">Данные для регистрации.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>результат регистрации с данными пользователя.</returns>
        /// <response code="200">успешный ответ.</response>
        /// <response code="400">ошибка валидации.</response>
        /// <response code="401">не авторизован.</response>
        /// <remarks>
        /// пример запроса:
        /// {
        ///   "email": "user@example.com",
        ///   "username": "user_01",
        ///   "password": "StrongPass1!",
        ///   "confirmPassword": "StrongPass1!",
        ///   "firstName": "Alex",
        ///   "lastName": "Ivanov",
        ///   "dateOfBirth": "2000-01-01",
        ///   "phoneNumber": "+12345678901"
        /// }
        /// пример ответа:
        /// {
        ///   "success": true,
        ///   "message": "Registration successful.",
        ///   "data": { "id": "...", "email": "user@example.com", "username": "user_01" }
        /// }
        /// mock токен подтверждения email возвращается в заголовке: X-Email-Confirmation-Token
        /// ошибки:
        /// 400 - ошибка валидации
        /// </remarks>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 400)]
        public async Task<ActionResult<ApiResponse<UserDto>>> Register(
            [FromBody] RegisterDto request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResult("Validation failed", validation.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var hash = _passwordHasher.HashPassword(request.Password, out var salt);
            var displayName = $"{request.FirstName} {request.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = request.Username;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Username = request.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = displayName,
                DateOfBirth = request.DateOfBirth,
                PhoneNumber = request.PhoneNumber,
                Country = "Unknown",
                IsActive = true,
                IsEmailConfirmed = false,
                IsDeleted = false
            };

            var userRole = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);
            if (userRole != null)
            {
                _dbContext.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = userRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }

            _dbContext.Users.Add(user);
            var confirmationToken = GenerateSecureToken();
            await UpsertTokenClaimAsync(
                user.Id,
                EmailConfirmationClaimType,
                HashToken(confirmationToken),
                DateTime.UtcNow.AddDays(2),
                cancellationToken,
                saveChanges: false);
            await _dbContext.SaveChangesAsync(cancellationToken);

            Response.Headers["X-Email-Confirmation-Token"] = confirmationToken;
            await EnqueueAuditAsync(SecurityEventType.Register, user.Id, user.Email, true, null, cancellationToken);
            if (userRole != null)
            {
                await EnqueueAuditAsync(SecurityEventType.RoleAssigned, user.Id, user.Email, true,
                    new { Role = userRole.Name }, cancellationToken);
            }

            var dto = MapUserDto(user);
            return Ok(ApiResponse<UserDto>.SuccessResult(dto, "Registration successful."));
        }

        /// <summary>
        /// выполняет вход и возвращает access/refresh токены.
        /// </summary>
        /// <param name="request">Данные для входа.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>токены и профиль пользователя.</returns>
        /// <response code="200">успешный ответ.</response>
        /// <response code="400">ошибка валидации.</response>
        /// <response code="401">не авторизован.</response>
        /// <remarks>
        /// пример запроса:
        /// { "emailOrUsername": "user@example.com", "password": "StrongPass1!" }
        /// пример ответа:
        /// { "success": true, "data": { "accessToken": "...", "refreshToken": "..." } }
        /// ошибки:
        /// 400 - ошибка валидации
        /// 401 - неверные учетные данные
        /// </remarks>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 401)]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
            [FromBody] LoginDto request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<AuthResponse>.ErrorResult("Validation failed", validation.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername, cancellationToken);

            if (user == null)
            {
                await EnqueueAuditAsync(SecurityEventType.FailedLogin, null, request.EmailOrUsername, false,
                    new { Reason = "UserNotFound" }, cancellationToken);
                await CheckSuspiciousFailedLoginsAsync(request.EmailOrUsername, GetIpAddress(), cancellationToken);
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid credentials"));
            }

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                await EnqueueAuditAsync(SecurityEventType.FailedLogin, user.Id, user.Email, false,
                    new { Reason = "InvalidPassword" }, cancellationToken);
                await CheckSuspiciousFailedLoginsAsync(user.Email, GetIpAddress(), cancellationToken);
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid credentials"));
            }

            if (!user.IsActive || user.IsDeleted)
            {
                await EnqueueAuditAsync(SecurityEventType.FailedLogin, user.Id, user.Email, false,
                    new { Reason = "InactiveOrDeleted" }, cancellationToken);
                await CheckSuspiciousFailedLoginsAsync(user.Email, GetIpAddress(), cancellationToken);
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("User is inactive"));
            }

            var accessExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
            var refreshExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            var roles = user.UserRoles.Select(ur => ur.Role?.Name).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            var accessToken = _jwtTokenService.CreateAccessToken(BuildClaims(user, roles), accessExpiry);
            var refreshToken = GenerateSecureToken();
            var lastIp = await _dbContext.UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => s.IpAddress)
                .FirstOrDefaultAsync(cancellationToken);

            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RefreshTokenHash = HashToken(refreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = refreshExpiry,
                DeviceInfo = Request.Headers.UserAgent.ToString(),
                IpAddress = GetIpAddress(),
                IsRevoked = false
            };

            user.LastLoginAt = DateTime.UtcNow;
            _dbContext.UserSessions.Add(session);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await EnqueueAuditAsync(SecurityEventType.Login, user.Id, user.Email, true, null, cancellationToken);
            if (!string.IsNullOrWhiteSpace(lastIp) && !string.Equals(lastIp, session.IpAddress, StringComparison.OrdinalIgnoreCase))
            {
                await EnqueueAuditAsync(SecurityEventType.UnusualIpAddress, user.Id, user.Email, false,
                    new { PreviousIp = lastIp, CurrentIp = session.IpAddress }, cancellationToken);
            }

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = accessExpiry,
                RefreshTokenExpiry = refreshExpiry,
                User = MapUserDto(user)
            };

            return Ok(ApiResponse<AuthResponse>.SuccessResult(response, "Login successful"));
        }

        /// <summary>
        /// обновляет access token по refresh token.
        /// </summary>
        /// <param name="request">Данные для обновления токена.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>новая пара токенов.</returns>
        /// <response code="200">успешный ответ.</response>
        /// <response code="400">ошибка валидации.</response>
        /// <response code="401">не авторизован.</response>
        /// <remarks>
        /// пример запроса:
        /// { "accessToken": "expired-token", "refreshToken": "refresh-token" }
        /// пример ответа:
        /// { "success": true, "data": { "accessToken": "...", "refreshToken": "..." } }
        /// ошибки:
        /// 400 - ошибка валидации
        /// 401 - неверный токен
        /// </remarks>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 401)]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(
            [FromBody] RefreshTokenDto request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _refreshValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<AuthResponse>.ErrorResult("Validation failed", validation.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            ClaimsPrincipal principal;
            try
            {
                principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            }
            catch
            {
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid token"));
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid token"));
            }

            var user = await _dbContext.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                await EnqueueAuditAsync(SecurityEventType.TokenRefresh, userId, null, false,
                    new { Reason = "UserNotFound" }, cancellationToken);
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid token"));
            }

            var sessions = await _dbContext.UserSessions
                .Where(s => s.UserId == userId && !s.IsRevoked)
                .ToListAsync(cancellationToken);

            var session = sessions.FirstOrDefault(s => VerifyToken(request.RefreshToken, s.RefreshTokenHash));
            if (session == null)
            {
                await EnqueueAuditAsync(SecurityEventType.TokenRefresh, user.Id, user.Email, false,
                    new { Reason = "RefreshTokenNotFound" }, cancellationToken);
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid refresh token"));
            }

            if (session.ExpiresAt <= DateTime.UtcNow)
            {
                await EnqueueAuditAsync(SecurityEventType.ExpiredTokenUsed, user.Id, user.Email, false,
                    new { Reason = "RefreshTokenExpired" }, cancellationToken);
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid refresh token"));
            }

            session.IsRevoked = true;
            var newRefreshToken = GenerateSecureToken();
            var refreshExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            var newSession = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RefreshTokenHash = HashToken(newRefreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = refreshExpiry,
                DeviceInfo = Request.Headers.UserAgent.ToString(),
                IpAddress = GetIpAddress(),
                IsRevoked = false
            };

            var accessExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
            var roles = user.UserRoles.Select(ur => ur.Role?.Name).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            var accessToken = _jwtTokenService.CreateAccessToken(BuildClaims(user, roles), accessExpiry);

            _dbContext.UserSessions.Add(newSession);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await EnqueueAuditAsync(SecurityEventType.TokenRefresh, user.Id, user.Email, true, null, cancellationToken);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = accessExpiry,
                RefreshTokenExpiry = refreshExpiry,
                User = MapUserDto(user)
            };

            return Ok(ApiResponse<AuthResponse>.SuccessResult(response, "Token refreshed"));
        }

        /// <summary>
        /// выход из системы с отзывом всех сессий пользователя.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>результат операции выхода.</returns>
        /// <response code="200">успешный ответ.</response>
        /// <response code="400">ошибка валидации.</response>
        /// <response code="401">не авторизован.</response>
        /// <remarks>
        /// ошибки:
        /// 401 - неверный пользователь
        /// 403 - доступ запрещен
        /// </remarks>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> Logout(CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<bool>.ErrorResult("Invalid user"));
            }

            var sessions = await _dbContext.UserSessions
                .Where(s => s.UserId == userId && !s.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var session in sessions)
            {
                session.IsRevoked = true;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await EnqueueAuditAsync(SecurityEventType.Logout, userId, null, true, null, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(true, "Logged out"));
        }

        /// <summary>отзывает конкретный refresh token для текущего пользователя.</summary>
        /// <remarks>
        /// пример запроса:
        /// { "refreshToken": "token" }
        /// ошибки:
        /// 400 - ошибка валидации
        /// 401 - неверный пользователь
        /// 403 - доступ запрещен
        /// </remarks>
        [HttpPost("revoke")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeToken(
            [FromBody] RevokeTokenDto request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _revokeValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Validation failed", validation.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<bool>.ErrorResult("Invalid user"));
            }

            var sessions = await _dbContext.UserSessions
                .Where(s => s.UserId == userId && !s.IsRevoked)
                .ToListAsync(cancellationToken);

            var revoked = false;
            foreach (var session in sessions)
            {
                if (VerifyToken(request.RefreshToken, session.RefreshTokenHash))
                {
                    session.IsRevoked = true;
                    revoked = true;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(revoked, revoked ? "Token revoked" : "Token not found"));
        }

        /// <summary>запускает восстановление пароля для пользователя.</summary>
        /// <remarks>
        /// пример запроса:
        /// { "email": "user@example.com" }
        /// пример ответа:
        /// { "success": true, "message": "If the email exists, a reset link was sent" }
        /// </remarks>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword(
            [FromBody] ForgotPasswordDto request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _forgotValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Validation failed", validation.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
            if (user != null)
            {
                var resetToken = GenerateSecureToken();
                var resetHash = HashToken(resetToken);
                await UpsertTokenClaimAsync(user.Id, PasswordResetClaimType, resetHash, DateTime.UtcNow.AddHours(2), cancellationToken);
            }

            await EnqueueAuditAsync(SecurityEventType.PasswordResetRequested, user?.Id, request.Email, true, null, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(true, "If the email exists, a reset link was sent"));
        }

        /// <summary>сбрасывает пароль по reset токену.</summary>
        /// <remarks>
        /// пример запроса:
        /// { "token": "reset-token", "newPassword": "StrongPass1!", "confirmNewPassword": "StrongPass1!" }
        /// ошибки:
        /// 400 - неверный токен или ошибка валидации
        /// </remarks>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(
            [FromBody] ResetPasswordDto request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _resetValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Validation failed", validation.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var user = await FindUserByTokenAsync(PasswordResetClaimType, request.Token, cancellationToken);
            if (user == null)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid or expired token"));
            }

            var hash = _passwordHasher.HashPassword(request.NewPassword, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            await RemoveTokenClaimAsync(user.Id, PasswordResetClaimType, cancellationToken);
            await RevokeUserSessionsAsync(user.Id, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await EnqueueAuditAsync(SecurityEventType.PasswordReset, user.Id, user.Email, true, null, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(true, "Password reset successful"));
        }

        /// <summary>меняет пароль для текущего пользователя.</summary>
        /// <remarks>
        /// пример запроса:
        /// { "currentPassword": "OldPass1!", "newPassword": "NewPass1!", "confirmNewPassword": "NewPass1!" }
        /// ошибки:
        /// 400 - ошибка валидации или неверный пароль
        /// 401 - неверный пользователь
        /// 403 - доступ запрещен
        /// </remarks>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
            [FromBody] ChangePasswordDto request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _changeValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Validation failed", validation.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<bool>.ErrorResult("Invalid user"));
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null || !_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid password"));
            }

            var hash = _passwordHasher.HashPassword(request.NewPassword, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            await RevokeUserSessionsAsync(user.Id, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await EnqueueAuditAsync(SecurityEventType.PasswordChanged, user.Id, user.Email, true, null, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(true, "Password changed"));
        }

        /// <summary>
        /// возвращает профиль текущего пользователя.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>профиль пользователя.</returns>
        /// <response code="200">успешный ответ.</response>
        /// <response code="400">ошибка валидации.</response>
        /// <response code="401">не авторизован.</response>
        /// <remarks>
        /// ошибки:
        /// 401 - неверный пользователь
        /// 404 - пользователь не найден
        /// 403 - доступ запрещен
        /// </remarks>
        [HttpGet("profile")]
        [Authorize(Roles = "User,Admin")]
        public async Task<ActionResult<ApiResponse<UserDto>>> Profile(CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<UserDto>.ErrorResult("Invalid user"));
            }

            var user = await _dbContext.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
            }

            return Ok(ApiResponse<UserDto>.SuccessResult(MapUserDto(user), "Profile loaded"));
        }

        /// <summary>
        /// подтверждает email пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="token">Токен подтверждения.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>результат подтверждения email.</returns>
        /// <response code="200">успешный ответ.</response>
        /// <response code="400">ошибка валидации.</response>
        /// <response code="401">не авторизован.</response>
        /// <remarks>
        /// пример запроса:
        /// /api/auth/confirm-email?userId=...&amp;token=...
        /// ошибки:
        /// 400 - неверный токен
        /// </remarks>
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ConfirmEmail(
            [FromQuery] Guid userId,
            [FromQuery] string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid token"));
            }

            var user = await FindUserByTokenAsync(EmailConfirmationClaimType, token, cancellationToken, userId);
            if (user == null)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid token"));
            }

            user.IsEmailConfirmed = true;
            await RemoveTokenClaimAsync(user.Id, EmailConfirmationClaimType, cancellationToken, saveChanges: false);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await EnqueueAuditAsync(SecurityEventType.EmailConfirmed, user.Id, user.Email, true, null, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(true, "Email confirmed"));
        }

        private Task EnqueueAuditAsync(
            SecurityEventType eventType,
            Guid? userId,
            string? email,
            bool success,
            object? details,
            CancellationToken cancellationToken)
        {
            var payload = details == null ? null : JsonSerializer.Serialize(details);
            return _auditService.EnqueueAsync(new SecurityAuditEntry(
                eventType,
                userId,
                email,
                GetIpAddress(),
                GetUserAgent(),
                success,
                payload,
                DateTime.UtcNow), cancellationToken);
        }

        private async Task CheckSuspiciousFailedLoginsAsync(string? identifier, string ipAddress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return;
            }

            var cutoff = DateTime.UtcNow.AddMinutes(-15);
            var recentFailures = await _dbContext.SecurityAuditLogs
                .AsNoTracking()
                .Where(x => x.EventType == SecurityEventType.FailedLogin && x.Timestamp >= cutoff && x.IpAddress == ipAddress)
                .CountAsync(cancellationToken);

            if (recentFailures + 1 >= 3)
            {
                await EnqueueAuditAsync(SecurityEventType.SuspiciousActivity, null, identifier, false,
                    new { Reason = "MultipleFailedLogins", Count = recentFailures + 1 }, cancellationToken);
            }
        }

        private IEnumerable<Claim> BuildClaims(User user, IEnumerable<string?> roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Username),
                new("FirstName", user.FirstName ?? string.Empty),
                new("LastName", user.LastName ?? string.Empty),
                new("EmailConfirmed", user.IsEmailConfirmed.ToString().ToLowerInvariant())
            };

            if (user.DateOfBirth.HasValue)
            {
                claims.Add(new Claim("DateOfBirth", user.DateOfBirth.Value.ToString("O")));
            }

            foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                claims.Add(new Claim(ClaimTypes.Role, role!));
            }

            foreach (var userClaim in user.Claims.Where(c => !ReservedClaimTypes.Contains(c.ClaimType)))
            {
                claims.Add(new Claim(userClaim.ClaimType, userClaim.ClaimValue));
            }

            return claims;
        }

        private static string GenerateSecureToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string HashToken(string token)
        {
            return BCrypt.Net.BCrypt.HashPassword(token);
        }

        private static bool VerifyToken(string token, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(token, hash);
        }

        private async Task UpsertTokenClaimAsync(
            Guid userId,
            string claimType,
            string tokenHash,
            DateTime expiresAt,
            CancellationToken cancellationToken,
            bool saveChanges = true)
        {
            await RemoveTokenClaimAsync(userId, claimType, cancellationToken, saveChanges: false);
            var payload = JsonSerializer.Serialize(new TokenClaimPayload(tokenHash, expiresAt));
            _dbContext.UserClaims.Add(new DomainUserClaim
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ClaimType = claimType,
                ClaimValue = payload
            });
            if (saveChanges)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task RemoveTokenClaimAsync(
            Guid userId,
            string claimType,
            CancellationToken cancellationToken,
            bool saveChanges = true)
        {
            var existing = await _dbContext.UserClaims
                .Where(c => c.UserId == userId && c.ClaimType == claimType)
                .ToListAsync(cancellationToken);
            if (existing.Count == 0)
            {
                return;
            }

            _dbContext.UserClaims.RemoveRange(existing);
            if (saveChanges)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<User?> FindUserByTokenAsync(string claimType, string token, CancellationToken cancellationToken, Guid? userId = null)
        {
            var query = _dbContext.Users
                .Include(u => u.Claims)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(u => u.Id == userId);
            }

            var users = await query.ToListAsync(cancellationToken);
            foreach (var user in users)
            {
                var claim = user.Claims.FirstOrDefault(c => c.ClaimType == claimType);
                if (claim == null)
                {
                    continue;
                }

                if (!TryParseTokenClaim(claim.ClaimValue, out var payload))
                {
                    continue;
                }

                if (payload.ExpiresAt < DateTime.UtcNow)
                {
                    continue;
                }

                if (VerifyToken(token, payload.Hash))
                {
                    return user;
                }
            }

            return null;
        }

        private static bool TryParseTokenClaim(string value, out TokenClaimPayload payload)
        {
            try
            {
                payload = JsonSerializer.Deserialize<TokenClaimPayload>(value) ?? new TokenClaimPayload(string.Empty, DateTime.MinValue);
                return !string.IsNullOrWhiteSpace(payload.Hash);
            }
            catch
            {
                payload = new TokenClaimPayload(string.Empty, DateTime.MinValue);
                return false;
            }
        }

        private async Task RevokeUserSessionsAsync(Guid userId, CancellationToken cancellationToken)
        {
            var sessions = await _dbContext.UserSessions
                .Where(s => s.UserId == userId && !s.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var session in sessions)
            {
                session.IsRevoked = true;
            }
        }

        private UserDto MapUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                DisplayName = user.DisplayName,
                ProfileImage = user.ProfileImage,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                FavoriteGenres = user.FavoriteGenres,
                ListenTimeMinutes = user.ListenTimeMinutes,
                LastLoginAt = user.LastLoginAt,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsActive = user.IsActive
            };
        }

        private Guid? GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }

        private string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers.UserAgent.ToString();
        }

        private sealed record TokenClaimPayload(string Hash, DateTime ExpiresAt);
    }
}
