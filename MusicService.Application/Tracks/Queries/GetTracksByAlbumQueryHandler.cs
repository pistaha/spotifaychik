using AutoMapper;
using MediatR;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Tracks.Queries
{
    public class GetTracksByAlbumQueryHandler : IRequestHandler<GetTracksByAlbumQuery, List<TrackDto>>
    {
        private readonly ITrackRepository _trackRepository;
        private readonly IMapper _mapper;

        public GetTracksByAlbumQueryHandler(
            ITrackRepository trackRepository,
            IMapper mapper)
        {
            _trackRepository = trackRepository;
            _mapper = mapper;
        }

        public async Task<List<TrackDto>> Handle(GetTracksByAlbumQuery request, CancellationToken cancellationToken)
        {
            var tracks = await _trackRepository.GetTracksByAlbumAsync(request.AlbumId, cancellationToken);
            
            if (request.SortByTrackNumber)
            {
                tracks = tracks.OrderBy(t => t.TrackNumber).ToList();
            }

            return _mapper.Map<List<TrackDto>>(tracks);
        }
    }
}