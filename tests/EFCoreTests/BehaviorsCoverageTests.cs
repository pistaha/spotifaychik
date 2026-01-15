using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MusicService.Application.Common.Behaviors;
using Xunit;

namespace Tests.EFCoreTests
{
    public class BehaviorsCoverageTests
    {
        private sealed class DummyRequest : IRequest<string>
        {
            public string Name { get; init; } = string.Empty;
        }

        [Fact]
        public async Task LoggingBehavior_ShouldReturnResponse_OnSuccess()
        {
            var behavior = new LoggingBehavior<DummyRequest, string>(NullLogger<LoggingBehavior<DummyRequest, string>>.Instance);
            var request = new DummyRequest { Name = "ok" };

            var result = await behavior.Handle(request, () => Task.FromResult("done"), CancellationToken.None);

            result.Should().Be("done");
        }

        [Fact]
        public async Task LoggingBehavior_ShouldRethrow_OnFailure()
        {
            var behavior = new LoggingBehavior<DummyRequest, string>(NullLogger<LoggingBehavior<DummyRequest, string>>.Instance);
            var request = new DummyRequest { Name = "fail" };

            Func<Task> act = () => behavior.Handle(request, () => throw new InvalidOperationException("boom"), CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ValidationBehavior_ShouldPass_WhenValid()
        {
            var validator = new InlineValidator<DummyRequest>();
            validator.RuleFor(x => x.Name).NotEmpty();
            var behavior = new ValidationBehavior<DummyRequest, string>(new[] { validator });

            var result = await behavior.Handle(new DummyRequest { Name = "ok" }, () => Task.FromResult("done"), CancellationToken.None);

            result.Should().Be("done");
        }
    }
}
