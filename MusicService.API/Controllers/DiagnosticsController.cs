using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MusicService.API.Diagnostics;
using MusicService.API.Infrastructure;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/diagnostics")]
    [ApiExplorerSettings(GroupName = "Diagnostics")]
    public sealed class DiagnosticsController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly MemoryStressOptions _options;
        private readonly string _instanceId;

        public DiagnosticsController(
            IWebHostEnvironment environment,
            IHostApplicationLifetime lifetime,
            ILogger<DiagnosticsController> logger,
            IOptions<MemoryStressOptions> options,
            IConfiguration configuration)
        {
            _environment = environment;
            _lifetime = lifetime;
            _logger = logger;
            _options = options.Value;
            _instanceId = InstanceIdResolver.Resolve(configuration);
        }

        [HttpPost("memory/oom")]
        [AllowAnonymous]
        public IActionResult TriggerOutOfMemory()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            if (!_options.Enabled)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = "Memory stress test is disabled."
                });
            }

            var chunkSizeBytes = _options.ChunkSizeMb * 1024 * 1024;

            _logger.LogWarning(
                "Starting fatal memory stress test with chunk size {ChunkSizeMb} MB and max chunks {MaxChunks}.",
                _options.ChunkSizeMb,
                _options.MaxChunks);

            Response.Headers["X-Instance-Id"] = _instanceId;

            _ = Task.Run(async () =>
            {
                await Task.Delay(750);
                ExhaustMemoryUntilProcessDies(chunkSizeBytes, _options.MaxChunks);
            });

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                message = "Out of memory",
                instanceId = _instanceId
            });
        }

        [HttpPost("graceful-shutdown")]
        [AllowAnonymous]
        public IActionResult GracefulShutdown()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            _logger.LogWarning(
                "Graceful shutdown test requested for instance {InstanceId}. Application will stop after response.",
                _instanceId);

            Response.Headers["X-Instance-Id"] = _instanceId;

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);

                _logger.LogWarning(
                    "Calling StopApplication() for graceful shutdown test on instance {InstanceId}.",
                    _instanceId);

                _lifetime.StopApplication();
            });

            return Ok(new
            {
                success = true,
                message = "Graceful shutdown test started. Application will stop in 1 second.",
                instanceId = _instanceId
            });
        }

        private void ExhaustMemoryUntilProcessDies(int chunkSizeBytes, int maxChunks)
        {
            var allocations = new List<nint>();

            for (var chunkIndex = 0; chunkIndex < maxChunks; chunkIndex++)
            {
                var memory = Marshal.AllocHGlobal(chunkSizeBytes);
                allocations.Add(memory);

                for (var pageOffset = 0; pageOffset < chunkSizeBytes; pageOffset += 4096)
                {
                    Marshal.WriteByte(memory, pageOffset, 0x7F);
                }

                _logger.LogInformation(
                    "Allocated chunk {ChunkNumber}/{MaxChunks}. Approximate allocated memory: {AllocatedMb} MB.",
                    chunkIndex + 1,
                    maxChunks,
                    chunkIndex * _options.ChunkSizeMb + _options.ChunkSizeMb);
            }

            while (true)
            {
                var memory = Marshal.AllocHGlobal(chunkSizeBytes);
                allocations.Add(memory);

                for (var pageOffset = 0; pageOffset < chunkSizeBytes; pageOffset += 4096)
                {
                    Marshal.WriteByte(memory, pageOffset, 0x7F);
                }
            }
        }
    }
}   
