using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace MusicService.API.Infrastructure;

internal static partial class InstanceIdResolver
{
    private const string DockerSocketPath = "/var/run/docker.sock";
    private static readonly Lazy<string> CachedInstanceId = new(ResolveCore);

    public static string Resolve(IConfiguration configuration)
    {
        var configured = configuration["App:InstanceId"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return CachedInstanceId.Value;
    }

    private static string ResolveCore()
    {
        var fallback = Environment.MachineName;

        try
        {
            if (!File.Exists(DockerSocketPath))
            {
                return fallback;
            }

            using var handler = new SocketsHttpHandler
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new System.Net.Sockets.Socket(
                        System.Net.Sockets.AddressFamily.Unix,
                        System.Net.Sockets.SocketType.Stream,
                        System.Net.Sockets.ProtocolType.Unspecified);

                    var endpoint = new System.Net.Sockets.UnixDomainSocketEndPoint(DockerSocketPath);
                    await socket.ConnectAsync(endpoint, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
            };

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };

            using var response = client.GetAsync($"/containers/{fallback}/json").GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return fallback;
            }

            using var payload = response.Content.ReadAsStream();
            using var document = JsonDocument.Parse(payload);

            if (!document.RootElement.TryGetProperty("Name", out var nameProperty))
            {
                return fallback;
            }

            var containerName = nameProperty.GetString()?.Trim('/');
            if (string.IsNullOrWhiteSpace(containerName))
            {
                return fallback;
            }

            var match = AppReplicaRegex().Match(containerName);
            return match.Success ? match.Value : containerName;
        }
        catch
        {
            return fallback;
        }
    }

    [GeneratedRegex(@"app-\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex AppReplicaRegex();
}
