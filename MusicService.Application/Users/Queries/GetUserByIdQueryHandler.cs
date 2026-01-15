using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Users.Dtos;
using MusicService.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Users.Queries
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        private readonly IMusicServiceDbContext _dbContext;

        public GetUserByIdQueryHandler(
            IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == request.UserId)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Username = u.Username,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    ProfileImage = u.ProfileImage,
                    Country = u.Country,
                    FavoriteGenres = u.FavoriteGenres,
                    ListenTimeMinutes = u.ListenTimeMinutes,
                    LastLogin = u.LastLogin,
                    PlaylistCount = u.CreatedPlaylists.Count,
                    FollowingCount = u.FollowedArtists.Count + u.FollowedPlaylists.Count,
                    FollowerCount = u.Friends.Count
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
