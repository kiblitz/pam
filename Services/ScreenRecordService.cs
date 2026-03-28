using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Pam.Services;

public class ScreenRecordService
{
    private CancellationTokenSource? _cts;
    private Rect _region;
    private string? _framesDir;
    private int _frameCount;
    private DateTime _startTime;

    public bool IsRecording => _cts != null;
    public TimeSpan Elapsed => IsRecording ? DateTime.UtcNow - _startTime : TimeSpan.Zero;

    public void StartRecording(Rect region)
    {
        _region = region;
        _frameCount = 0;
        _startTime = DateTime.UtcNow;

        _framesDir = Path.Combine(Path.GetTempPath(), "Pam", $"rec-{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(_framesDir);

        _cts = new CancellationTokenSource();
        Task.Run(() => CaptureLoop(_cts.Token));
    }

    public string? StopRecording()
    {
        if (_cts == null || _framesDir == null)
            return null;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;

        if (_frameCount == 0)
            return null;

        var outputDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pam");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, $"recording-{DateTime.Now:yyyyMMdd-HHmmss}.mp4");

        var ffmpegPath = FindFfmpeg();
        if (ffmpegPath == null)
            return null;

        var args = $"-framerate 10 -i \"{_framesDir}\\frame_%06d.png\" -c:v libx264 -pix_fmt yuv420p -y \"{outputPath}\"";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            }
        };

        process.Start();
        process.WaitForExit(30_000);

        // Clean up frames
        try { Directory.Delete(_framesDir, true); } catch { }
        _framesDir = null;

        return process.ExitCode == 0 ? outputPath : null;
    }

    public BitmapSource? GetFirstFrame()
    {
        if (_framesDir == null) return null;
        var first = Path.Combine(_framesDir, "frame_000000.png");
        if (!File.Exists(first)) return null;

        using var stream = new FileStream(first, FileMode.Open, FileAccess.Read, FileShare.Read);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return decoder.Frames[0];
    }

    private void CaptureLoop(CancellationToken ct)
    {
        var x = (int)_region.X;
        var y = (int)_region.Y;
        var w = (int)_region.Width;
        var h = (int)_region.Height;

        // ffmpeg needs even dimensions for libx264
        w = w % 2 == 0 ? w : w - 1;
        h = h % 2 == 0 ? h : h - 1;

        if (w <= 0 || h <= 0) return;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h));
                }

                var path = Path.Combine(_framesDir!, $"frame_{_frameCount:D6}.png");
                bitmap.Save(path, ImageFormat.Png);
                Interlocked.Increment(ref _frameCount);

                // ~10 fps
                Thread.Sleep(100);
            }
            catch
            {
                break;
            }
        }
    }

    private static string? FindFfmpeg()
    {
        // Check common locations
        string[] candidates =
        [
            "ffmpeg",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WinGet", "Links", "ffmpeg.exe"),
            @"C:\ProgramData\chocolatey\bin\ffmpeg.exe",
        ];

        foreach (var candidate in candidates)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = candidate,
                        Arguments = "-version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    }
                };
                process.Start();
                process.WaitForExit(3000);
                if (process.ExitCode == 0) return candidate;
            }
            catch { }
        }

        return null;
    }
}
