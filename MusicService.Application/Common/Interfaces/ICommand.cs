using MediatR;

namespace MusicService.Application.Common.Interfaces
{
    public interface ICommand : IRequest
    {
    }

    public interface ICommand<TResponse> : IRequest<TResponse>
    {
    }
}
