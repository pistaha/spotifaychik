using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MusicService.API.Configuration;
using Xunit;

namespace Tests.MusicService.API.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    [Fact]
    public void InvalidModelStateResponseFactory_ShouldReturnBadRequestWithErrors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiServices(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = provider },
            RouteData = new(),
            ActionDescriptor = new()
        };
        actionContext.ModelState.AddModelError("Title", "Title is required");
        actionContext.ModelState.AddModelError("Description", "Description too long");

        var result = options.InvalidModelStateResponseFactory!(actionContext);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value!.GetType().GetProperty("Errors")?.GetValue(badRequest.Value) as IEnumerable<string>;
        response.Should().Contain("Title is required").And.Contain("Description too long");
    }
}
