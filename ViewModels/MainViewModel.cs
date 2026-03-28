using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using Pam.Services;

namespace Pam.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ClaudeUsageService _service;
    private readonly DispatcherTimer _pollTimer;
    private readonly DispatcherTimer _countdownTimer;
    private DateTimeOffset _fiveHourReset;
    private DateTimeOffset _sevenDayReset;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(20);

    public MainViewModel()
    {
        _service = new ClaudeUsageService();

        _pollTimer = new DispatcherTimer { Interval = PollInterval };
        _pollTimer.Tick += async (_, _) => await RefreshNow();

        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (_, _) => UpdateCountdowns();
    }

    // --- Bindable properties ---

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

    // --- Lifecycle ---

    public void Start()
    {
        _pollTimer.Start();
        _countdownTimer.Start();
        _ = RefreshNow();
    }

    public void Stop()
    {
        _pollTimer.Stop();
        _countdownTimer.Stop();
    }

    public async Task RefreshNow()
    {
        try
        {
            StatusText = "Fetching...";
            var info = await _service.GetUsageAsync();

            FiveHourUtil = info.FiveHour.Utilization;
            SevenDayUtil = info.SevenDay.Utilization;
            _fiveHourReset = info.FiveHour.ResetsAt;
            _sevenDayReset = info.SevenDay.ResetsAt;

            UpdateCountdowns();
            StatusText = $"Updated {DateTimeOffset.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
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
        _service.Dispose();
    }
}
