using System;
using System.Collections.Generic;
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
    private List<(byte[] PngData, int DelayMs)>? _frames;
    private Rect _region;
    private DateTime _lastFrameTime;

    public bool IsRecording => _cts != null;

    public void StartRecording(Rect region)
    {
        _region = region;
        _frames = [];
        _cts = new CancellationTokenSource();
        _lastFrameTime = DateTime.UtcNow;

        Task.Run(() => CaptureLoop(_cts.Token));
    }

    public BitmapSource? StopRecording()
    {
        if (_cts == null || _frames == null)
            return null;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;

        if (_frames.Count == 0)
            return null;

        var gifBytes = EncodeGif(_frames);
        _frames = null;

        // Copy gif to clipboard as an image (first frame) and also save the gif
        var gifPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Pam", $"recording-{DateTime.Now:yyyyMMdd-HHmmss}.gif");

        var dir = Path.GetDirectoryName(gifPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(gifPath, gifBytes);

        // Return first frame as preview
        using var stream = new MemoryStream(gifBytes);
        var decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return decoder.Frames.Count > 0 ? decoder.Frames[0] : null;
    }

    private void CaptureLoop(CancellationToken ct)
    {
        var x = (int)_region.X;
        var y = (int)_region.Y;
        var w = (int)_region.Width;
        var h = (int)_region.Height;

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

                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);

                var now = DateTime.UtcNow;
                var delayMs = (int)(now - _lastFrameTime).TotalMilliseconds;
                _lastFrameTime = now;

                lock (_frames!)
                {
                    _frames.Add((ms.ToArray(), Math.Max(delayMs, 33)));
                }

                // ~10 fps
                Thread.Sleep(100);
            }
            catch
            {
                break;
            }
        }
    }

    private static byte[] EncodeGif(List<(byte[] PngData, int DelayMs)> frames)
    {
        using var output = new MemoryStream();
        var encoder = new GifBitmapEncoder();

        foreach (var (pngData, _) in frames)
        {
            using var ms = new MemoryStream(pngData);
            var pngDecoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (pngDecoder.Frames.Count > 0)
                encoder.Frames.Add(pngDecoder.Frames[0]);
        }

        encoder.Save(output);
        return output.ToArray();
    }
}
