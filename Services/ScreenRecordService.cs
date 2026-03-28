using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Pam.Services;

public class ScreenRecordService
{
    private Process? _ffmpegProcess;
    private string? _outputPath;
    private DateTime _startTime;

    public bool IsRecording => _ffmpegProcess != null && !_ffmpegProcess.HasExited;
    public TimeSpan Elapsed => IsRecording ? DateTime.UtcNow - _startTime : TimeSpan.Zero;

    public bool StartRecording(Rect region, string? audioDevice)
    {
        var ffmpegPath = FindFfmpeg();
        if (ffmpegPath == null)
            return false;

        _startTime = DateTime.UtcNow;

        var outputDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pam");
        Directory.CreateDirectory(outputDir);
        _outputPath = Path.Combine(outputDir, $"recording-{DateTime.Now:yyyyMMdd-HHmmss}.mp4");

        var w = (int)region.Width;
        var h = (int)region.Height;
        // libx264 needs even dimensions
        w = w % 2 == 0 ? w : w - 1;
        h = h % 2 == 0 ? h : h - 1;

        var args = $"-f gdigrab -framerate 15 -offset_x {(int)region.X} -offset_y {(int)region.Y} -video_size {w}x{h} -i desktop";

        if (!string.IsNullOrEmpty(audioDevice))
            args += $" -f dshow -i audio=\"{audioDevice}\"";

        args += $" -c:v libx264 -crf 20 -preset fast -pix_fmt yuv420p";

        if (!string.IsNullOrEmpty(audioDevice))
            args += " -c:a aac";

        args += $" -y \"{_outputPath}\"";

        _ffmpegProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
            }
        };
        _ffmpegProcess.Start();
        return true;
    }

    public string? StopRecording()
    {
        if (_ffmpegProcess == null)
            return null;

        // Send 'q' to ffmpeg to gracefully stop
        try
        {
            _ffmpegProcess.StandardInput.Write("q");
            _ffmpegProcess.StandardInput.Flush();
        }
        catch { }

        if (!_ffmpegProcess.WaitForExit(10_000))
        {
            try { _ffmpegProcess.Kill(); } catch { }
        }

        var exitCode = _ffmpegProcess.ExitCode;
        _ffmpegProcess.Dispose();
        _ffmpegProcess = null;

        if (_outputPath != null && File.Exists(_outputPath) && new FileInfo(_outputPath).Length > 0)
            return _outputPath;

        return null;
    }

    public static List<string> ListAudioDevices()
    {
        var ffmpegPath = FindFfmpeg();
        if (ffmpegPath == null)
            return [];

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-f dshow -list_devices true -i dummy",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            }
        };
        process.Start();
        var output = process.StandardError.ReadToEnd();
        process.WaitForExit(5000);

        var devices = new List<string>();
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            if (line.Contains("(audio)"))
            {
                var start = line.IndexOf('"');
                var end = line.IndexOf('"', start + 1);
                if (start >= 0 && end > start)
                    devices.Add(line[(start + 1)..end]);
            }
        }

        return devices;
    }

    private static string? FindFfmpeg()
    {
        string[] candidates = ["ffmpeg", "ffmpeg.exe"];

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
