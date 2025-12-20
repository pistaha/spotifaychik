using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MusicService.Application.Search
{
    public static class SearchModule
    {
        public static IServiceCollection AddSearchModule(this IServiceCollection services)
        {
            services.AddMediatR(cfg => 
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            
            return services;
        }
    }
}