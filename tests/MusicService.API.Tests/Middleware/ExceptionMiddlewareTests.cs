using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MusicService.API.Configuration;
using Moq;
using Xunit;

namespace Tests.MusicService.API.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    [Fact]
    public void InvalidModelStateResponseFactory_ShouldReturnBadRequestWithErrors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:EnableDevelopmentAuth"] = "true"
            })
            .Build();
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);
        services.AddApiServices(configuration, env.Object);
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
