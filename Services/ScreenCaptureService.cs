using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Pam.Services;

public static class ScreenCaptureService
{
    public static BitmapSource CaptureRegion(Rect region)
    {
        var x = (int)region.X;
        var y = (int)region.Y;
        var w = (int)region.Width;
        var h = (int)region.Height;

        if (w <= 0 || h <= 0)
            throw new ArgumentException("Region has zero area.");

        using var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h));
        }

        return BitmapToBitmapSource(bitmap);
    }

    private static BitmapSource BitmapToBitmapSource(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return decoder.Frames[0];
    }
}
