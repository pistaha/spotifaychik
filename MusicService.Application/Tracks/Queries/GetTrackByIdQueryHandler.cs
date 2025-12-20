using AutoMapper;
using MediatR;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Tracks.Queries
{
    public class GetTrackByIdQueryHandler : IRequestHandler<GetTrackByIdQuery, TrackDto?>
    {
        private readonly ITrackRepository _trackRepository;
        private readonly IMapper _mapper;

        public GetTrackByIdQueryHandler(
            ITrackRepository trackRepository,
            IMapper mapper)
        {
            _trackRepository = trackRepository;
            _mapper = mapper;
        }

        public async Task<TrackDto?> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
        {
            var track = await _trackRepository.GetByIdAsync(request.TrackId, cancellationToken);
            return track != null ? _mapper.Map<TrackDto>(track) : null;
        }
    }
}