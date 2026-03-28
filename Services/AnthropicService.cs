using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Pam.Models;

namespace Pam.Services;

public class ClaudeUsageService : IDisposable
{
    private readonly HttpClient _client;
    private static readonly string CredentialsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".claude", ".credentials.json");

    public ClaudeUsageService()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://api.anthropic.com/")
        };
    }

    public async Task<UsageInfo> GetUsageAsync()
    {
        var token = ReadAccessToken();

        var request = new HttpRequestMessage(HttpMethod.Get, "api/oauth/usage");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("anthropic-beta", "oauth-2025-04-20");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return ParseUsageResponse(json);
    }

    private static string ReadAccessToken()
    {
        if (!File.Exists(CredentialsPath))
            throw new FileNotFoundException(
                $"Claude credentials not found at {CredentialsPath}. Make sure Claude Code is installed and authenticated.");

        var json = File.ReadAllText(CredentialsPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("claudeAiOauth", out var oauth) &&
            oauth.TryGetProperty("accessToken", out var token))
        {
            return token.GetString()
                ?? throw new InvalidOperationException("Access token is null.");
        }

        throw new InvalidOperationException("Could not find accessToken in credentials file.");
    }

    private static UsageInfo ParseUsageResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new UsageInfo
        {
            FiveHour = ParseWindow(root, "five_hour"),
            SevenDay = ParseWindow(root, "seven_day"),
        };
    }

    private static UsageWindow ParseWindow(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var window))
            return new UsageWindow();

        var utilization = window.TryGetProperty("utilization", out var u)
            ? u.GetDouble() : 0;
        var resetsAt = window.TryGetProperty("resets_at", out var r)
            ? DateTimeOffset.Parse(r.GetString()!) : DateTimeOffset.UtcNow;

        return new UsageWindow
        {
            Utilization = utilization,
            ResetsAt = resetsAt,
        };
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
