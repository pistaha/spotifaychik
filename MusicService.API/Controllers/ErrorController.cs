using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace MusicService.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("error")]
    public class ErrorController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public ErrorController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult HandleError()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var error = feature?.Error;

            return Problem(
                title: "An unexpected error occurred.",
                detail: _environment.IsDevelopment() ? error?.Message : null,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
