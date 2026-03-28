# Pam — Desktop Butler App

## Architecture

- **Framework**: .NET 8, WPF (C#)
- **Pattern**: MVVM-lite (manual INotifyPropertyChanged, no framework)
- **Window**: Transparent, borderless, always-on-top, rounded rectangle via `AllowsTransparency` + `Border` with `CornerRadius`

## Project Structure

```
Pam.sln / Pam.csproj     — Solution and project files
App.xaml/cs               — Application entry point
MainWindow.xaml/cs        — The floating widget window (drag, context menu)
Models/
  RateLimitInfo.cs        — UsageInfo / UsageWindow data models
Services/
  AnthropicService.cs     — ClaudeUsageService: reads OAuth token, calls /api/oauth/usage
ViewModels/
  MainViewModel.cs        — Polls usage every 20min, countdown timers, data binding
```

## Key Decisions

- **Usage data source**: Undocumented `GET https://api.anthropic.com/api/oauth/usage` endpoint with OAuth bearer token from `~/.claude/.credentials.json`. No official API exists for Claude Max subscription usage.
- **Poll interval**: 20 minutes. The endpoint is aggressively rate-limited (~5 requests per token before 429 for hours), so we poll conservatively.
- **OAuth token**: Read from `~/.claude/.credentials.json` (`claudeAiOauth.accessToken`). Token can go stale after ~8 hours if Claude Code refreshes it in memory without writing back.

## Building & Running

```
dotnet build
dotnet run
```

Requires: .NET 8 SDK (x64), Claude Code authenticated (for the credentials file).
