using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Pam.Services;

public class ScreenRecordService
{
    private CancellationTokenSource? _cts;
    private Rect _region;
    private int _frameCount;
    private DateTime _startTime;
    private Process? _ffmpegProcess;
    private string? _outputPath;

    public bool IsRecording => _cts != null;
    public TimeSpan Elapsed => IsRecording ? DateTime.UtcNow - _startTime : TimeSpan.Zero;

    public void StartRecording(Rect region)
    {
        _region = region;
        _frameCount = 0;
        _startTime = DateTime.UtcNow;

        var outputDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pam");
        Directory.CreateDirectory(outputDir);
        _outputPath = Path.Combine(outputDir, $"recording-{DateTime.Now:yyyyMMdd-HHmmss}.mp4");

        var w = (int)_region.Width;
        var h = (int)_region.Height;
        // ffmpeg needs even dimensions
        w = w % 2 == 0 ? w : w - 1;
        h = h % 2 == 0 ? h : h - 1;

        var ffmpegPath = FindFfmpeg();
        if (ffmpegPath == null)
            return;

        // Pipe raw BGRA frames to ffmpeg via stdin
        var args = $"-f rawvideo -pix_fmt bgra -s {w}x{h} -r 10 -i pipe:0 -c:v libx264 -pix_fmt yuv420p -preset ultrafast -y \"{_outputPath}\"";

        _ffmpegProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
            }
        };
        _ffmpegProcess.Start();

        _cts = new CancellationTokenSource();
        Task.Run(() => CaptureLoop(_cts.Token, w, h));
    }

    public string? StopRecording()
    {
        if (_cts == null || _ffmpegProcess == null)
            return null;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;

        // Close stdin to signal ffmpeg to finish encoding
        try { _ffmpegProcess.StandardInput.BaseStream.Close(); } catch { }
        _ffmpegProcess.WaitForExit(15_000);

        var exitCode = _ffmpegProcess.ExitCode;
        _ffmpegProcess.Dispose();
        _ffmpegProcess = null;

        if (exitCode != 0 || _frameCount == 0)
            return null;

        return _outputPath;
    }

    private void CaptureLoop(CancellationToken ct, int w, int h)
    {
        if (w <= 0 || h <= 0) return;

        var x = (int)_region.X;
        var y = (int)_region.Y;
        var stride = w * 4; // BGRA = 4 bytes per pixel
        var bufferSize = stride * h;
        var buffer = new byte[bufferSize];

        var stream = _ffmpegProcess?.StandardInput.BaseStream;
        if (stream == null) return;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h));
                }

                // Extract raw pixel data
                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, w, h),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                try
                {
                    // Copy row by row in case strides differ
                    for (var row = 0; row < h; row++)
                    {
                        Marshal.Copy(bmpData.Scan0 + row * bmpData.Stride, buffer, row * stride, stride);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                stream.Write(buffer, 0, bufferSize);
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
        string[] candidates = ["ffmpeg", "ffmpeg.exe"];

        // Search WinGet packages
        var wingetPackages = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WinGet", "Packages");

        if (Directory.Exists(wingetPackages))
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(wingetPackages, "ffmpeg.exe", SearchOption.AllDirectories))
                {
                    candidates = [file, .. candidates];
                    break;
                }
            }
            catch { }
        }

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
                        RedirectStandardError = true,
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
