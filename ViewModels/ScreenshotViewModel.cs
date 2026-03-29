using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Pam.Services;
using Pam.Views;

namespace Pam.ViewModels;

public class ScreenshotViewModel : INotifyPropertyChanged
{
    private readonly ScreenRecordService _recorder = new();
    private readonly DispatcherTimer _elapsedTimer;

    private RecordingBorder? _borderWindow;

    public ScreenshotViewModel()
    {
        _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _elapsedTimer.Tick += (_, _) => UpdateElapsed();
    }

    private int _delaySeconds;
    public int DelaySeconds
    {
        get => _delaySeconds;
        set => SetField(ref _delaySeconds, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    private bool _isRecording;
    public bool IsRecording
    {
        get => _isRecording;
        private set => SetField(ref _isRecording, value);
    }

    private int _fps = 90;
    public int Fps
    {
        get => _fps;
        set => SetField(ref _fps, value);
    }


    public async Task CaptureScreenshot(Window owner)
    {
        owner.Hide();
        await Task.Delay(150);

        var region = SelectRegion();
        if (region == null)
        {
            owner.Show();
            return;
        }

        if (DelaySeconds > 0)
            await RunCountdown(DelaySeconds);

        try
        {
            var image = ScreenCaptureService.CaptureRegion(region.Value);
            Clipboard.SetImage(image);
            StatusText = "Copied to clipboard!";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }

        owner.Show();
    }

    public async Task StartOrStopRecording(Window owner)
    {
        if (IsRecording)
        {
            _elapsedTimer.Stop();
            IsRecording = false;
            StatusText = "Finishing...";

            _borderWindow?.Close();
            _borderWindow = null;

            var outputPath = await Task.Run(() => _recorder.StopRecording());

            if (outputPath != null)
            {
                Clipboard.SetFileDropList([outputPath]);
                StatusText = $"Saved!";
            }
            else
            {
                StatusText = "Recording failed (is ffmpeg installed?)";
            }

            owner.Show();
            return;
        }

        owner.Hide();
        await Task.Delay(150);

        var region = SelectRegion();
        if (region == null)
        {
            owner.Show();
            return;
        }

        if (DelaySeconds > 0)
            await RunCountdown(DelaySeconds);

        var started = _recorder.StartRecording(region.Value, includeAudio: true, fps: Fps);

        if (!started)
        {
            StatusText = "Failed to start (is ffmpeg installed?)";
            owner.Show();
            return;
        }

        IsRecording = true;
        _elapsedTimer.Start();

        _borderWindow = new RecordingBorder(region.Value);
        _borderWindow.Show();

        owner.Show();
    }

    private void UpdateElapsed()
    {
        var elapsed = _recorder.Elapsed;
        StatusText = $"Recording {elapsed:mm\\:ss\\.f}";
    }

    private static Rect? SelectRegion()
    {
        var selector = new RegionSelector();
        var result = selector.ShowDialog();

        if (result == true && selector.RegionSelected)
            return selector.SelectedRegion;
        return null;
    }

    private async Task RunCountdown(int seconds)
    {
        for (var i = seconds; i > 0; i--)
        {
            StatusText = $"Capturing in {i}...";
            await Task.Delay(1000);
        }
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
}
