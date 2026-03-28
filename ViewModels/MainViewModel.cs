using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using Pam.Services;

namespace Pam.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ClaudeUsageService _oauthService;
    private readonly LocalUsageService _localService;
    private readonly DispatcherTimer _oauthPollTimer;
    private readonly DispatcherTimer _localPollTimer;
    private readonly DispatcherTimer _countdownTimer;
    private DateTimeOffset _fiveHourReset;
    private DateTimeOffset _sevenDayReset;

    // Used to convert local token counts to approximate percentages.
    // We calibrate these when we get an OAuth response.
    private double _tokensPerPercent5h;
    private double _tokensPerPercent7d;

    public MainViewModel()
    {
        _oauthService = new ClaudeUsageService();
        _localService = new LocalUsageService();

        _oauthPollTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _oauthPollTimer.Tick += async (_, _) => await RefreshOAuth();

        _localPollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _localPollTimer.Tick += (_, _) => RefreshLocal();

        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (_, _) => UpdateCountdowns();
    }

    // --- Official OAuth data ---

    private double _fiveHourUtil;
    public double FiveHourUtil
    {
        get => _fiveHourUtil;
        private set => SetField(ref _fiveHourUtil, value);
    }

    private double _sevenDayUtil;
    public double SevenDayUtil
    {
        get => _sevenDayUtil;
        private set => SetField(ref _sevenDayUtil, value);
    }

    // --- Local approximation data ---

    private double _fiveHourLocalUtil;
    public double FiveHourLocalUtil
    {
        get => _fiveHourLocalUtil;
        private set => SetField(ref _fiveHourLocalUtil, value);
    }

    private double _sevenDayLocalUtil;
    public double SevenDayLocalUtil
    {
        get => _sevenDayLocalUtil;
        private set => SetField(ref _sevenDayLocalUtil, value);
    }

    private string _fiveHourRemainingText = "--";
    public string FiveHourRemainingText
    {
        get => _fiveHourRemainingText;
        private set => SetField(ref _fiveHourRemainingText, value);
    }

    private string _sevenDayRemainingText = "--";
    public string SevenDayRemainingText
    {
        get => _sevenDayRemainingText;
        private set => SetField(ref _sevenDayRemainingText, value);
    }

    private string _statusText = "Waiting...";
    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    private bool _hasOAuthData;
    public bool HasOAuthData
    {
        get => _hasOAuthData;
        private set => SetField(ref _hasOAuthData, value);
    }

    // --- Lifecycle ---

    public void Start()
    {
        _oauthPollTimer.Start();
        _localPollTimer.Start();
        _countdownTimer.Start();
        RefreshLocal();
        _ = RefreshOAuth();
    }

    public void Stop()
    {
        _oauthPollTimer.Stop();
        _localPollTimer.Stop();
        _countdownTimer.Stop();
    }

    public async Task RefreshAll()
    {
        RefreshLocal();
        await RefreshOAuth();
    }

    private async Task RefreshOAuth()
    {
        try
        {
            StatusText = "Fetching...";
            var info = await _oauthService.GetUsageAsync();

            FiveHourUtil = info.FiveHour.Utilization;
            SevenDayUtil = info.SevenDay.Utilization;
            _fiveHourReset = info.FiveHour.ResetsAt;
            _sevenDayReset = info.SevenDay.ResetsAt;
            HasOAuthData = true;

            // Calibrate local-to-percent ratio
            var local = _localService.GetLocalUsage();
            if (info.FiveHour.Utilization > 0 && local.TokensLast5Hours > 0)
                _tokensPerPercent5h = local.TokensLast5Hours / info.FiveHour.Utilization;
            if (info.SevenDay.Utilization > 0 && local.TokensLast7Days > 0)
                _tokensPerPercent7d = local.TokensLast7Days / info.SevenDay.Utilization;

            UpdateCountdowns();
            StatusText = $"Updated {DateTimeOffset.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText = $"OAuth: {ex.Message}";
        }
    }

    private void RefreshLocal()
    {
        try
        {
            var local = _localService.GetLocalUsage();

            // If we have calibration data, estimate percentages
            if (_tokensPerPercent5h > 0)
                FiveHourLocalUtil = Math.Min(100, local.TokensLast5Hours / _tokensPerPercent5h);
            if (_tokensPerPercent7d > 0)
                SevenDayLocalUtil = Math.Min(100, local.TokensLast7Days / _tokensPerPercent7d);
        }
        catch
        {
            // Local scan failed — not critical
        }
    }

    private void UpdateCountdowns()
    {
        FiveHourRemainingText = FormatRemaining(_fiveHourReset);
        SevenDayRemainingText = FormatRemaining(_sevenDayReset);
    }

    private static string FormatRemaining(DateTimeOffset resetTime)
    {
        var remaining = resetTime - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
            return "now";

        if (remaining.TotalDays >= 1)
            return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        if (remaining.TotalMinutes >= 1)
            return $"{remaining.Minutes}m {remaining.Seconds}s";
        return $"{remaining.Seconds}s";
    }

    // --- INotifyPropertyChanged ---

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    public void Dispose()
    {
        Stop();
        _oauthService.Dispose();
    }
}
