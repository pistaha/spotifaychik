using System;
using MusicService.API.Configuration;
using MusicService.Application;
using MusicService.Infrastructure;
using MusicService.Infrastructure.Configuration;
using FluentMigrator.Runner;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5L * 1024 * 1024 * 1024;
});

// Настройка стандартного логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Регистрация слоев приложения
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// API сервисы
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

// Настройка HTTP-клиента
builder.Services.AddHttpClient("default", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Конфигурация middleware
app.UseApiConfiguration(app.Environment);

using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
    app.Logger.LogInformation("Database migrations applied successfully");
}

// Запуск приложения
app.Logger.LogInformation("Music Service API started successfully!");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Listening on: {Urls}", string.Join(", ", app.Urls));

try
{
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
