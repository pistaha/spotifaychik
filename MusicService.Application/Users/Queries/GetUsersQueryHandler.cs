using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using MusicService.Application.Users.Dtos;

namespace MusicService.Application.Users.Queries
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public GetUsersQueryHandler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllAsync(cancellationToken);
            IEnumerable<Domain.Entities.User> query = users;

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(u =>
                    u.Username.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(u.DisplayName) && u.DisplayName.Contains(request.Search, StringComparison.OrdinalIgnoreCase)) ||
                    u.Email.Contains(request.Search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.Country))
            {
                query = query.Where(u => u.Country.Equals(request.Country, StringComparison.OrdinalIgnoreCase));
            }

            query = query.OrderByDescending(u => u.CreatedAt);

            var totalCount = query.Count();
            var items = query
                .Skip(Math.Max(0, (request.Page - 1) * request.PageSize))
                .Take(request.PageSize)
                .ToList();

            var dtoItems = _mapper.Map<List<UserDto>>(items);
            return new PagedResult<UserDto>(dtoItems, totalCount, request.Page, request.PageSize);
        }
    }
}
