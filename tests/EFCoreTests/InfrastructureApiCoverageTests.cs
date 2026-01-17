using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MusicService.API.Configuration;
using MusicService.Application;
using MusicService.Application.Common.Behaviors;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Users.Commands;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Configuration;
using MusicService.Infrastructure.Persistence;
using MusicService.Infrastructure.Security;
using Xunit;

namespace Tests.EFCoreTests
{
    public class InfrastructureApiCoverageTests
    {
        [Fact]
        public void ApplicationDependencyInjection_ShouldRegisterServices()
        {
            var services = new ServiceCollection();
            services.AddDbContext<MusicServiceDbContext>(options =>
                options.UseSqlite("DataSource=:memory:"));
            services.AddScoped<IMusicServiceDbContext>(provider =>
                provider.GetRequiredService<MusicServiceDbContext>());
            services.AddApplication();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<AutoMapper.IMapper>().Should().NotBeNull();
            provider.GetRequiredService<IValidator<CreateUserCommand>>().Should().NotBeNull();
        }

        [Fact]
        public void InfrastructureDependencyInjection_ShouldRegisterServices()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ConnectionString"] = "Host=localhost;Database=test;Username=postgres;Password=postgres"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddInfrastructure(config);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IPasswordHasher>().Should().NotBeNull();
        }

        [Fact]
        public void ApiDependencyInjection_ShouldConfigureOptions()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:EnableDevelopmentAuth"] = "true"
                })
                .Build();
            services.AddApiServices(config, new TestWebHostEnvironment());

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            options.InvalidModelStateResponseFactory.Should().NotBeNull();
        }

        [Fact]
        public void ApiDependencyInjection_ShouldConfigureJwtAuth_WhenConfigured()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Issuer"] = "MusicService",
                    ["JwtSettings:Audience"] = "MusicServiceUsers",
                    ["JwtSettings:Secret"] = "test-secret-key-min-32-chars-long"
                })
                .Build();

            var env = new TestWebHostEnvironment { EnvironmentName = Environments.Production };
            services.AddApiServices(config, env);

            var provider = services.BuildServiceProvider();
            provider.Should().NotBeNull();
        }

        [Fact]
        public void ApiDependencyInjection_ShouldThrow_WhenJwtMissingInProduction()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:EnableDevelopmentAuth"] = "false"
                })
                .Build();

            var env = new TestWebHostEnvironment { EnvironmentName = Environments.Production };
            Action act = () => services.AddApiServices(config, env);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ApiDependencyInjection_ShouldConfigureMiddleware()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddDbContext<MusicServiceDbContext>(options =>
                options.UseSqlite("DataSource=:memory:"));
            builder.Services.AddScoped<IMusicServiceDbContext>(provider =>
                provider.GetRequiredService<MusicServiceDbContext>());
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:EnableDevelopmentAuth"] = "true"
            });
            builder.Environment.EnvironmentName = Environments.Development;
            builder.Services.AddApiServices(builder.Configuration, builder.Environment);

            var app = builder.Build();
            ApiDependencyInjection.UseApiConfiguration(app, app.Environment).Should().NotBeNull();
        }

        [Fact]
        public async Task DbContext_SaveChangesAsync_ShouldAssignIdsAndTimestamps()
        {
            var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
            var user = new User
            {
                Username = "db_user",
                Email = "db_user@music.local",
                PasswordHash = "hash",
                DisplayName = "Db User",
                Country = "US"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            user.Id.Should().NotBe(Guid.Empty);
            user.CreatedAt.Should().NotBe(default);
            user.UpdatedAt.Should().NotBe(default);
        }

        [Fact]
        public void PasswordHasher_ShouldHashAndValidateInput()
        {
            var hasher = new BcryptPasswordHasher();
            var hash = hasher.HashPassword("password", out _);

            hash.Should().NotBeNullOrWhiteSpace();
            Action act = () => hasher.HashPassword(" ", out _);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public async Task ValidationBehavior_ShouldThrowOnFailure()
        {
            var validator = new InlineValidator<DummyRequest>();
            validator.RuleFor(x => x.Name).NotEmpty();

            var behavior = new ValidationBehavior<DummyRequest, bool>(new[] { validator });
            var request = new DummyRequest { Name = string.Empty };

            Func<Task> act = () => behavior.Handle(request, () => Task.FromResult(true), CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();
        }

        private sealed class DummyRequest : MediatR.IRequest<bool>
        {
            public string Name { get; init; } = string.Empty;
        }

        private sealed class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;
            public string ApplicationName { get; set; } = "MusicService.API";
            public string WebRootPath { get; set; } = Path.GetTempPath();
            public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(Path.GetTempPath());
            public string ContentRootPath { get; set; } = Path.GetTempPath();
            public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Path.GetTempPath());
        }
    }
}
