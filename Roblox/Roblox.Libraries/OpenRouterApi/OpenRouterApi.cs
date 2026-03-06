using System.Text;
using System.Text.Json;
using Roblox.Logging;

namespace Roblox.Libraries.OpenRouterApi;

public class OpenRouterReviewResult
{
    public long userId { get; set; }
    public bool accept { get; set; }
    public string punishment { get; set; } = string.Empty;
    public string banNote { get; set; } = string.Empty;
    public string internalReason { get; set; } = string.Empty;
}

public class OpenRouterApi
{
    private readonly HttpClient _client;
    private const string Model = "stepfun/step-3.5-flash:free";

    public OpenRouterApi(string apiKey)
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://openrouter.ai/api/v1/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<List<OpenRouterReviewResult>?> ReviewReport(string systemPrompt, string userContent)
    {
        var requestBody = new
        {
            model = Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _client.PostAsync("chat/completions", content);
        }
        catch (HttpRequestException ex)
        {
            Writer.Info(LogGroup.AiReportReview, "HTTP request to OpenRouter failed: {0}", ex.Message);
            return null;
        }
        catch (TaskCanceledException)
        {
            Writer.Info(LogGroup.AiReportReview, "OpenRouter request timed out");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            Writer.Info(LogGroup.AiReportReview, "OpenRouter returned non-success status {0}", response.StatusCode);
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync();

        string? messageContent;
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            Writer.Info(LogGroup.AiReportReview, "Failed to parse OpenRouter response structure: {0}", ex.Message);
            return null;
        }

        if (string.IsNullOrWhiteSpace(messageContent))
            return null;

        var arrayStart = messageContent.IndexOf('[');
        var arrayEnd = messageContent.LastIndexOf(']');
        if (arrayStart == -1 || arrayEnd == -1 || arrayEnd <= arrayStart)
        {
            Writer.Info(LogGroup.AiReportReview, "No JSON array found in model response");
            return null;
        }

        var extracted = messageContent.Substring(arrayStart, arrayEnd - arrayStart + 1);

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<OpenRouterReviewResult>>(extracted, options);
        }
        catch (JsonException ex)
        {
            Writer.Info(LogGroup.AiReportReview, "Failed to deserialize AI result JSON: {0}", ex.Message);
            return null;
        }
    }
}
