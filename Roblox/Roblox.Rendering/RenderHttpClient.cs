using System.Net.Http.Json;

namespace Roblox.Rendering;

public static class RenderHttpClient
{
    private static readonly object Gate = new();
    private static HttpClient _client = CreateClient("http://127.0.0.1:3521/", string.Empty);

    public static void Configure(string baseUrl, string authorization)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("Arbiter render URL is required", nameof(baseUrl));
        lock (Gate)
        {
            var old = _client;
            _client = CreateClient(baseUrl, authorization);
            old.Dispose();
        }
    }

    public static void Configure(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (client.BaseAddress == null) throw new ArgumentException("The render client must have a base address", nameof(client));
        lock (Gate) { var old = _client; _client = client; if (!ReferenceEquals(old, client)) old.Dispose(); }
    }

    public static async Task<RenderResult> SendAsync(RenderRequest request, CancellationToken cancellationToken)
    {
        HttpClient client;
        lock (Gate) client = _client;
        using var response = await client.PostAsJsonAsync("render", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<RenderErrorResponse>(cancellationToken: cancellationToken);
            throw new HttpRequestException(error?.Errors.FirstOrDefault()?.Message ?? $"Render failed with HTTP {(int)response.StatusCode}", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<RenderResult>(cancellationToken: cancellationToken)
               ?? throw new InvalidDataException("Arbiter returned an empty render response");
    }

    private static HttpClient CreateClient(string baseUrl, string authorization)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(75) };
        if (!string.IsNullOrWhiteSpace(authorization)) client.DefaultRequestHeaders.TryAddWithoutValidation("rblx-authorization", authorization);
        return client;
    }
}
