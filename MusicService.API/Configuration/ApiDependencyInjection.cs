using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using MusicService.Infrastructure.Persistence;
using System.Reflection;

namespace MusicService.API.Configuration
{
    public static class ApiDependencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Контроллеры с настройкой JSON
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                });

            // Настройка поведения API
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true; // Отключаем автоматическую валидацию ModelState
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                        .ToList();

                    return new BadRequestObjectResult(new
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                };
            });

            // Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Music Service API",
                    Version = "v1",
                    Description = "API для музыкального сервиса с управлением плейлистами, артистами и треками",
                    Contact = new OpenApiContact
                    {
                        Name = "Music Service Team",
                        Email = "support@musicservice.com"
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                // Включаем XML комментарии
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Добавляем поддержка авторизации в Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Группировка по блокам (контроллерам/области)
                c.TagActionsBy(api =>
                {
                    if (!string.IsNullOrWhiteSpace(api.GroupName))
                    {
                        return new[] { api.GroupName! };
                    }

                    if (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller) &&
                        !string.IsNullOrWhiteSpace(controller))
                    {
                        return new[] { controller };
                    }

                    return new[] { "General" };
                });

                c.DocInclusionPredicate((name, api) =>
                {
                    if (!string.IsNullOrWhiteSpace(api.GroupName))
                        return true;

                    // Включаем любые действия без явной группы
                    api.GroupName = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller)
                        ? controller
                        : "General";

                    return true;
                });
            });

            // CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });

                options.AddPolicy("ProductionCors", policy =>
                {
                    policy.WithOrigins(
                            "https://musicservice.com",
                            "https://www.musicservice.com",
                            "https://api.musicservice.com")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Health Checks
            services.AddHealthChecks()
                .AddDbContextCheck<MusicServiceDbContext>("database");

            // Response Compression
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            return services;
        }

        public static IApplicationBuilder UseApiConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Исключения и обработка ошибок
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => 
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Music Service API v1");
                    c.DisplayRequestDuration();
                    c.EnableFilter();
                    c.EnableDeepLinking();
                    c.EnableValidator();
                });
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseResponseCompression();
            app.UseRouting();
            
            // CORS
            app.UseCors(env.IsDevelopment() ? "AllowAll" : "ProductionCors");
            
            // Health Checks
            app.UseHealthChecks("/health");
            
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });

            return app;
        }
    }
}
