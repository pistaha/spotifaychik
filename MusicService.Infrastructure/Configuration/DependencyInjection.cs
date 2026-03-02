using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces;
using MusicService.Infrastructure.Persistence;
using MusicService.Infrastructure.Security;
using System;

namespace MusicService.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Сервисы безопасности
            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            services.Configure<SecurityAuditOptions>(configuration.GetSection("SecurityAudit"));
            services.AddSingleton<SecurityAuditQueue>();
            services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
            services.AddHostedService<SecurityAuditBackgroundService>();

            var connectionString = configuration["Database:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Database:ConnectionString is not configured.");
            }

            services.AddDbContext<MusicServiceDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IMusicServiceDbContext>(provider =>
                provider.GetRequiredService<MusicServiceDbContext>());

            services.AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(MusicServiceDbContext).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddFluentMigratorConsole());

            return services;
        }
    }
}
