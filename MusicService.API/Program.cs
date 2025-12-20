using System;
using System.IO;
using MusicService.API.Configuration;
using MusicService.API.HealthChecks;
using MusicService.Application;
using MusicService.Infrastructure;
using MusicService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Настройка стандартного логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Конфигурация приложения
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection("FileStorage"));

// Регистрация слоев приложения
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// API сервисы
builder.Services.AddApiServices(builder.Configuration);

// Настройка HTTP-клиента
builder.Services.AddHttpClient("default", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Конфигурация middleware
app.UseApiConfiguration(app.Environment);

// Создание директории для данных и файлов
using (var scope = app.Services.CreateScope())
{
    var options = scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
    
    if (!Directory.Exists(options.DataDirectory))
    {
        Directory.CreateDirectory(options.DataDirectory);
        app.Logger.LogInformation("Created data directory: {DataDirectory}", options.DataDirectory);
    }

    // Создание поддиректорий для бэкапов
    if (options.Backup.Enabled && !string.IsNullOrEmpty(options.Backup.BackupDirectory))
    {
        var backupDir = Path.Combine(options.DataDirectory, options.Backup.BackupDirectory);
        if (!Directory.Exists(backupDir))
        {
            Directory.CreateDirectory(backupDir);
            app.Logger.LogInformation("Created backup directory: {BackupDirectory}", backupDir);
        }
    }
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
