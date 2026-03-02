using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Users.Dtos;

namespace MusicService.Application.Users.Queries
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;

        public GetUsersQueryHandler(IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            IQueryable<Domain.Entities.User> query = _dbContext.Users.AsNoTracking()
                .Where(u => !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                var isPostgres = _dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
                if (isPostgres)
                {
                    query = query.Where(u =>
                        EF.Functions.ILike(u.Username, $"%{search}%") ||
                        (u.DisplayName != null && EF.Functions.ILike(u.DisplayName, $"%{search}%")) ||
                        EF.Functions.ILike(u.Email, $"%{search}%"));
                }
                else
                {
                    var searchLower = search.ToLowerInvariant();
                    query = query.Where(u =>
                        EF.Functions.Like(u.Username.ToLower(), $"%{searchLower}%") ||
                        (u.DisplayName != null && EF.Functions.Like(u.DisplayName.ToLower(), $"%{searchLower}%")) ||
                        EF.Functions.Like(u.Email.ToLower(), $"%{searchLower}%"));
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Country))
            {
                var country = request.Country.Trim();
                query = query.Where(u => u.Country == country);
            }

            query = query.OrderByDescending(u => u.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip(Math.Max(0, (request.Page - 1) * request.PageSize))
                .Take(request.PageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    ProfileImage = u.ProfileImage,
                    PhoneNumber = u.PhoneNumber,
                    Country = u.Country,
                    FavoriteGenres = u.FavoriteGenres,
                    ListenTimeMinutes = u.ListenTimeMinutes,
                    LastLoginAt = u.LastLoginAt,
                    IsEmailConfirmed = u.IsEmailConfirmed,
                    IsActive = u.IsActive,
                    PlaylistCount = u.CreatedPlaylists.Count,
                    FollowingCount = u.FollowedArtists.Count + u.FollowedPlaylists.Count,
                    FollowerCount = u.Friends.Count
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<UserDto>(items, totalCount, request.Page, request.PageSize);
        }
    }
}
