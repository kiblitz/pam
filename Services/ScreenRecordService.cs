using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using NAudio.Wave;

namespace Pam.Services;

public class ScreenRecordService
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pam");

    private Process? _ffmpegProcess;
    private WasapiLoopbackCapture? _audioCapture;
    private WaveFileWriter? _audioWriter;
    private string? _videoPath;
    private string? _audioPath;
    private string? _outputPath;
    private string? _logPath;
    private DateTime _startTime;

    public bool IsRecording => _ffmpegProcess != null && !_ffmpegProcess.HasExited;
    public TimeSpan Elapsed => IsRecording ? DateTime.UtcNow - _startTime : TimeSpan.Zero;

    public bool StartRecording(Rect region, bool includeAudio)
    {
        var ffmpegPath = FindFfmpeg();
        if (ffmpegPath == null)
            return false;

        _startTime = DateTime.UtcNow;

        var outputDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pam");
        Directory.CreateDirectory(outputDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        _outputPath = Path.Combine(outputDir, $"recording-{timestamp}.mp4");
        _videoPath = Path.Combine(outputDir, $"recording-{timestamp}-video.mp4");
        _audioPath = Path.Combine(outputDir, $"recording-{timestamp}-audio.wav");
        _logPath = Path.Combine(outputDir, $"recording-{timestamp}.log");

        var w = (int)region.Width;
        var h = (int)region.Height;
        // libx264 needs even dimensions
        w = w % 2 == 0 ? w : w - 1;
        h = h % 2 == 0 ? h : h - 1;

        // Start video-only capture with ffmpeg
        var videoTarget = includeAudio ? _videoPath : _outputPath;
        var args = $"-f gdigrab -framerate 60 -offset_x {(int)region.X} -offset_y {(int)region.Y} " +
                   $"-video_size {w}x{h} -i desktop " +
                   $"-c:v libx264 -crf 14 -preset ultrafast -pix_fmt yuv420p " +
                   $"-y \"{videoTarget}\"";

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

        // Drain stderr to log file in background to prevent buffer deadlock
        var logPath = _logPath;
        var proc = _ffmpegProcess;
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                using var log = new StreamWriter(logPath!, append: false);
                log.WriteLine($"[record] args: {args}");
                log.Flush();
                while (!proc.HasExited || !proc.StandardError.EndOfStream)
                {
                    var line = proc.StandardError.ReadLine();
                    if (line != null) { log.WriteLine(line); log.Flush(); }
                }
            }
            catch { }
        });

        // Start desktop audio capture with NAudio WASAPI loopback
        if (includeAudio)
        {
            try
            {
                _audioCapture = new WasapiLoopbackCapture();
                _audioWriter = new WaveFileWriter(_audioPath, _audioCapture.WaveFormat);

                _audioCapture.DataAvailable += (_, e) =>
                {
                    _audioWriter?.Write(e.Buffer, 0, e.BytesRecorded);
                };

                _audioCapture.StartRecording();
            }
            catch
            {
                // Audio capture failed — continue with video only
                _audioCapture?.Dispose();
                _audioCapture = null;
                _audioWriter?.Dispose();
                _audioWriter = null;
            }
        }

        return true;
    }

    public string? StopRecording()
    {
        if (_ffmpegProcess == null)
            return null;

        var hadAudio = _audioCapture != null;

        // Stop and dispose audio capture so the WAV file is finalized
        try { _audioCapture?.StopRecording(); } catch { }
        try { _audioWriter?.Dispose(); } catch { }
        try { _audioCapture?.Dispose(); } catch { }
        _audioWriter = null;
        _audioCapture = null;

        // Send 'q' to ffmpeg to gracefully stop video
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

        _ffmpegProcess.Dispose();
        _ffmpegProcess = null;

        // If we captured audio, mux video + audio together
        if (hadAudio && _videoPath != null && _audioPath != null &&
            File.Exists(_videoPath) && File.Exists(_audioPath))
        {
            var muxed = MuxVideoAudio(_videoPath, _audioPath, _outputPath!);

            // Clean up temp files
            try { File.Delete(_videoPath); } catch { }
            try { File.Delete(_audioPath); } catch { }

            if (muxed)
                return _outputPath;
        }

        // No audio — video file is the output
        if (_outputPath != null && File.Exists(_outputPath) && new FileInfo(_outputPath).Length > 0)
            return _outputPath;

        return null;
    }

    private bool MuxVideoAudio(string videoPath, string audioPath, string outputPath)
    {
        var ffmpegPath = FindFfmpeg();
        if (ffmpegPath == null) return false;

        var args = $"-i \"{videoPath}\" -i \"{audioPath}\" " +
                   $"-c:v copy -c:a aac -af loudnorm -map 0:v -map 1:a " +
                   $"-y \"{outputPath}\"";

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
        var muxStderr = process.StandardError.ReadToEnd();
        process.WaitForExit(30_000);

        // Append mux log
        try
        {
            if (_logPath != null)
            {
                File.AppendAllText(_logPath,
                    $"\n[mux] args: {args}\n[mux] exit: {process.ExitCode}\n{muxStderr}\n" +
                    $"[mux] video size: {(File.Exists(videoPath) ? new FileInfo(videoPath).Length : 0)}\n" +
                    $"[mux] audio size: {(File.Exists(audioPath) ? new FileInfo(audioPath).Length : 0)}\n" +
                    $"[mux] output size: {(File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0)}\n");
            }
        }
        catch { }

        return process.ExitCode == 0 &&
               File.Exists(outputPath) &&
               new FileInfo(outputPath).Length > 0;
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
