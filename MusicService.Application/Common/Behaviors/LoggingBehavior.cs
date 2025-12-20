using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Handling {RequestName} with request: {@Request}", requestName, request);

            try
            {
                var response = await next();
                
                stopwatch.Stop();
                _logger.LogInformation("Handled {RequestName} successfully in {ElapsedMilliseconds}ms", 
                    requestName, stopwatch.ElapsedMilliseconds);
                
                return response;
            }
            catch (System.Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error handling {RequestName} after {ElapsedMilliseconds}ms", 
                    requestName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}