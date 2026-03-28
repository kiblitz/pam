using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Pam.Models;

namespace Pam.Services;

/// <summary>
/// Reads Claude Code's local JSONL session files to estimate token usage
/// over the 5-hour and 7-day windows.
/// </summary>
public class LocalUsageService
{
    private static readonly string ClaudeDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".claude", "projects");

    public LocalUsageInfo GetLocalUsage()
    {
        var now = DateTimeOffset.UtcNow;
        var fiveHoursAgo = now.AddHours(-5);
        var sevenDaysAgo = now.AddDays(-7);

        long tokens5h = 0;
        long tokens7d = 0;

        foreach (var entry in EnumerateUsageEntries(sevenDaysAgo))
        {
            tokens7d += entry.TotalTokens;
            if (entry.Timestamp >= fiveHoursAgo)
                tokens5h += entry.TotalTokens;
        }

        return new LocalUsageInfo
        {
            TokensLast5Hours = tokens5h,
            TokensLast7Days = tokens7d,
        };
    }

    private static IEnumerable<TokenEntry> EnumerateUsageEntries(DateTimeOffset since)
    {
        if (!Directory.Exists(ClaudeDir))
            yield break;

        foreach (var projectDir in Directory.EnumerateDirectories(ClaudeDir))
        {
            foreach (var jsonlFile in Directory.EnumerateFiles(projectDir, "*.jsonl"))
            {
                foreach (var entry in ReadEntriesFromFile(jsonlFile, since))
                    yield return entry;
            }

            // Also check subagent files
            foreach (var subDir in Directory.EnumerateDirectories(projectDir, "*", SearchOption.AllDirectories))
            {
                foreach (var jsonlFile in Directory.EnumerateFiles(subDir, "*.jsonl"))
                {
                    foreach (var entry in ReadEntriesFromFile(jsonlFile, since))
                        yield return entry;
                }
            }
        }
    }

    private static IEnumerable<TokenEntry> ReadEntriesFromFile(string path, DateTimeOffset since)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            TokenEntry? entry = null;
            try
            {
                entry = ParseLine(line, since);
            }
            catch
            {
                // Skip malformed lines
            }

            if (entry != null)
                yield return entry;
        }
    }

    private static TokenEntry? ParseLine(string line, DateTimeOffset since)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        // Need a timestamp
        if (!root.TryGetProperty("timestamp", out var tsProp))
            return null;

        var ts = DateTimeOffset.Parse(tsProp.GetString()!);
        if (ts < since)
            return null;

        // Look for usage inside message object
        if (!root.TryGetProperty("message", out var msg))
            return null;
        if (!msg.TryGetProperty("usage", out var usage))
            return null;

        long total = 0;
        total += GetLong(usage, "input_tokens");
        total += GetLong(usage, "output_tokens");
        total += GetLong(usage, "cache_creation_input_tokens");
        total += GetLong(usage, "cache_read_input_tokens");

        if (total == 0)
            return null;

        return new TokenEntry { Timestamp = ts, TotalTokens = total };
    }

    private static long GetLong(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val) && val.TryGetInt64(out var n))
            return n;
        return 0;
    }

    private class TokenEntry
    {
        public DateTimeOffset Timestamp { get; init; }
        public long TotalTokens { get; init; }
    }
}
