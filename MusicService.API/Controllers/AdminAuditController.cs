using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicService.API.Models;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using MusicService.Domain.Entities;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/admin/audit")]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "CanViewAuditLogs")]
    public class AdminAuditController : ControllerBase
    {
        private static readonly HashSet<SecurityEventType> SuspiciousEvents = new()
        {
            SecurityEventType.FailedLogin,
            SecurityEventType.ExpiredTokenUsed,
            SecurityEventType.ResourceAccessDenied,
            SecurityEventType.SuspiciousActivity,
            SecurityEventType.UnusualIpAddress
        };

        private readonly IMusicServiceDbContext _dbContext;

        public AdminAuditController(IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<SecurityAuditLogDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<SecurityAuditLogDto>>>> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] SecurityEventType? eventType = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] bool? success = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = ApplyFilters(_dbContext.SecurityAuditLogs.AsNoTracking(), eventType, userId, success, from, to);
            var totalCount = await query.CountAsync(cancellationToken);
            var logs = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var items = logs.Select(MapToDto).ToList();

            var result = PagedResult<SecurityAuditLogDto>.Create(items, totalCount, page, pageSize);
            return Ok(ApiResponse<PagedResult<SecurityAuditLogDto>>.SuccessResult(result, "Audit logs retrieved"));
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<SecurityAuditLogDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<SecurityAuditLogDto>>>> GetUserAuditLogs(
            Guid userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.SecurityAuditLogs
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            var totalCount = await query.CountAsync(cancellationToken);
            var logs = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var items = logs.Select(MapToDto).ToList();

            var result = PagedResult<SecurityAuditLogDto>.Create(items, totalCount, page, pageSize);
            return Ok(ApiResponse<PagedResult<SecurityAuditLogDto>>.SuccessResult(result, "User audit logs retrieved"));
        }

        [HttpGet("suspicious")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<SecurityAuditLogDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<SecurityAuditLogDto>>>> GetSuspiciousAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.SecurityAuditLogs
                .AsNoTracking()
                .Where(x => SuspiciousEvents.Contains(x.EventType));

            var totalCount = await query.CountAsync(cancellationToken);
            var logs = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var items = logs.Select(MapToDto).ToList();

            var result = PagedResult<SecurityAuditLogDto>.Create(items, totalCount, page, pageSize);
            return Ok(ApiResponse<PagedResult<SecurityAuditLogDto>>.SuccessResult(result, "Suspicious audit logs retrieved"));
        }

        private static IQueryable<SecurityAuditLog> ApplyFilters(
            IQueryable<SecurityAuditLog> query,
            SecurityEventType? eventType,
            Guid? userId,
            bool? success,
            DateTime? from,
            DateTime? to)
        {
            if (eventType.HasValue)
            {
                query = query.Where(x => x.EventType == eventType);
            }

            if (userId.HasValue)
            {
                query = query.Where(x => x.UserId == userId);
            }

            if (success.HasValue)
            {
                query = query.Where(x => x.Success == success.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.Timestamp <= to.Value);
            }

            return query;
        }

        private static SecurityAuditLogDto MapToDto(SecurityAuditLog log)
        {
            return new SecurityAuditLogDto
            {
                Id = log.Id,
                EventType = log.EventType,
                UserId = log.UserId,
                Email = log.Email,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                Success = log.Success,
                Details = log.Details,
                Timestamp = log.Timestamp
            };
        }
    }
}
