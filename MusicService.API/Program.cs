using System;
using System.Diagnostics;
using MusicService.API.Configuration;
using MusicService.API.Infrastructure;
using MusicService.Application;
using MusicService.Infrastructure;
using MusicService.Infrastructure.Configuration;
using FluentMigrator.Runner;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();
var instanceId = InstanceIdResolver.Resolve(builder.Configuration);
var requestCounter = Metrics.CreateCounter(
    "http_requests_received_total",
    "Total HTTP requests processed by Music Service.",
    new CounterConfiguration
    {
        LabelNames = ["status"]
    });
var requestDuration = Metrics.CreateHistogram(
    "http_request_duration_seconds",
    "The duration of HTTP requests processed by Music Service.",
    new HistogramConfiguration
    {
        Buckets = Histogram.ExponentialBuckets(0.01, 2, 15)
    });

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

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    context.Response.Headers["X-Instance-Id"] = instanceId;
    try
    {
        await next();
    }
    finally
    {
        stopwatch.Stop();
        requestCounter
            .WithLabels(context.Response.StatusCode.ToString())
            .Inc();
        requestDuration.Observe(stopwatch.Elapsed.TotalSeconds);
    }
});

app.UseApiConfiguration(app.Environment);

app.MapGet("/", (HttpContext context) =>
{
    context.Response.Headers["X-Instance-Id"] = instanceId;

    return Results.Ok(new
    {
        message = "Music Service API is running",
        instanceId,
        machineName = Environment.MachineName,
        timestampUtc = DateTime.UtcNow
    });
});

app.MapMetrics("/metrics");

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
