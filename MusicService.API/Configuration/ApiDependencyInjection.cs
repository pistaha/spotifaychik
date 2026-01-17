using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MusicService.API.Authentication;
using MusicService.API.Authorization;
using MusicService.Infrastructure.Persistence;
using System;
using System.Reflection;
using System.Text;

namespace MusicService.API.Configuration
{
    public static class ApiDependencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
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
                    Scheme = "bearer",
                    BearerFormat = "JWT"
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
                          .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                          .WithHeaders("Authorization", "Content-Type")
                          .AllowCredentials();
                });
            });

            // Health Checks
            services.AddHealthChecks()
                .AddDbContextCheck<MusicServiceDbContext>("database");

            // Authentication & Authorization (JWT)
            var jwtSection = configuration.GetSection("JwtSettings");
            var jwtIssuer = jwtSection["Issuer"];
            var jwtAudience = jwtSection["Audience"];
            var jwtSecret = jwtSection["Secret"];
            var jwtConfigured = !string.IsNullOrWhiteSpace(jwtIssuer) &&
                                !string.IsNullOrWhiteSpace(jwtAudience) &&
                                !string.IsNullOrWhiteSpace(jwtSecret);

            if (jwtConfigured)
            {
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtIssuer,
                            ValidAudience = jwtAudience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret ?? throw new InvalidOperationException("JwtSettings:Secret is not configured."))),
                            ClockSkew = TimeSpan.FromMinutes(2)
                        };
                    });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireModeratorOrAdmin", policy => policy.RequireRole("Admin", "Moderator"));
                options.AddPolicy("RequireEmailConfirmed", policy => policy.RequireClaim("EmailConfirmed", "true"));
                options.AddPolicy("RequirePremiumSubscription", policy => policy.RequireClaim("SubscriptionLevel", "Premium", "Enterprise"));
                options.AddPolicy("RequireMinimumAge", policy => policy.Requirements.Add(new MinimumAgeRequirement(18)));
                options.AddPolicy("CanDeleteTracks", policy => policy.Requirements.Add(new PermissionRequirement("CanDeleteTracks")));
                options.AddPolicy("CanEditMetadata", policy => policy.Requirements.Add(new PermissionRequirement("CanEditMetadata")));
                options.AddPolicy("CanViewAuditLogs", policy => policy.Requirements.Add(new PermissionRequirement("CanViewAuditLogs")));
                options.AddPolicy("CanManageUsers", policy => policy.RequireRole("Admin"));
            });
            }
            else if ((env.IsDevelopment() || env.IsEnvironment("Test") || env.IsEnvironment("Testing")) &&
                     configuration.GetValue<bool>("Auth:EnableDevelopmentAuth"))
            {
                services.AddAuthentication("Development")
                    .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>("Development", _ => { });
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                    options.AddPolicy("RequireModeratorOrAdmin", policy => policy.RequireRole("Admin", "Moderator"));
                    options.AddPolicy("RequireEmailConfirmed", policy => policy.RequireClaim("EmailConfirmed", "true"));
                    options.AddPolicy("RequirePremiumSubscription", policy => policy.RequireClaim("SubscriptionLevel", "Premium", "Enterprise"));
                    options.AddPolicy("RequireMinimumAge", policy => policy.Requirements.Add(new MinimumAgeRequirement(18)));
                    options.AddPolicy("CanDeleteTracks", policy => policy.Requirements.Add(new PermissionRequirement("CanDeleteTracks")));
                    options.AddPolicy("CanEditMetadata", policy => policy.Requirements.Add(new PermissionRequirement("CanEditMetadata")));
                    options.AddPolicy("CanViewAuditLogs", policy => policy.Requirements.Add(new PermissionRequirement("CanViewAuditLogs")));
                    options.AddPolicy("CanManageUsers", policy => policy.RequireRole("Admin"));
                });
            }
            else
            {
                throw new InvalidOperationException("Jwt settings are not configured.");
            }

            services.Configure<JwtSettings>(jwtSection);
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthorizationHandler, MinimumAgeAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, ResourceOwnerAuthorizationHandler>();

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
            
            app.UseAuthentication();
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
