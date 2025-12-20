using MediatR;

namespace MusicService.Application.Common.Interfaces
{
    public interface IQuery<TResponse> : IRequest<TResponse>
    {
    }
}