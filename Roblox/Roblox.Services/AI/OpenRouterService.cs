using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roblox.Services.AI;

public class OpenRouterService : ServiceBase, IService
{
    private const string Endpoint = "https://openrouter.ai/api/v1/chat/completions";

    private static readonly HttpClient Http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private sealed class Message
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }

    private sealed class Request
    {
        [JsonPropertyName("model")] public string Model { get; set; } = "";
        [JsonPropertyName("messages")] public Message[] Messages { get; set; } = Array.Empty<Message>();
        [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; }
        [JsonPropertyName("temperature")] public double Temperature { get; set; } = 0.4;
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")] public Message? Message { get; set; }
    }

    private sealed class Response
    {
        [JsonPropertyName("choices")] public Choice[]? Choices { get; set; }
    }

    public async Task<string?> ChatAsync(string systemPrompt, string userPrompt, bool online = false, int maxTokens = 200)
    {
        var apiKey = Roblox.Configuration.OpenRouterApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("[warn] OpenRouter API key not configured");
            return null;
        }

        var model = AiPrompts.Model;
        if (online) model += ":online";

        var body = new Request
        {
            Model = model,
            Messages = new[]
            {
                new Message { Role = "system", Content = systemPrompt },
                new Message { Role = "user", Content = userPrompt },
            },
            MaxTokens = maxTokens,
        };

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine("[warn] OpenRouter HTTP {0}", (int)resp.StatusCode);
                return null;
            }

            var parsed = await resp.Content.ReadFromJsonAsync<Response>();
            var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
        catch (Exception e)
        {
            Console.WriteLine("[warn] OpenRouter call failed: {0}", e.Message);
            return null;
        }
    }

    public bool IsThreadSafe() => true;
    public bool IsReusable() => false;
}
