using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pam.Views;

public partial class RegionSelector : Window
{
    private Point _startLocal;
    private bool _isDragging;

    public Rect SelectedRegion { get; private set; }
    public bool RegionSelected { get; private set; }

    public RegionSelector()
    {
        InitializeComponent();

        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
        KeyDown += OnKeyDown;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _startLocal = e.GetPosition(OverlayCanvas);
        _isDragging = true;
        SelectionRect.Visibility = Visibility.Visible;
        InstructionText.Visibility = Visibility.Collapsed;
        Mouse.Capture(this);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var current = e.GetPosition(OverlayCanvas);
        UpdateSelectionRect(_startLocal, current);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        Mouse.Capture(null);

        var endLocal = e.GetPosition(OverlayCanvas);
        var x = Math.Min(_startLocal.X, endLocal.X);
        var y = Math.Min(_startLocal.Y, endLocal.Y);
        var w = Math.Abs(endLocal.X - _startLocal.X);
        var h = Math.Abs(endLocal.Y - _startLocal.Y);

        if (w > 5 && h > 5)
        {
            // Convert local rect to screen coordinates for capture
            var topLeft = OverlayCanvas.PointToScreen(new Point(x, y));
            var bottomRight = OverlayCanvas.PointToScreen(new Point(x + w, y + h));
            SelectedRegion = new Rect(topLeft, bottomRight);
            RegionSelected = true;
            DialogResult = true;
            Close();
        }
    }

    private void UpdateSelectionRect(Point start, Point end)
    {
        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        var w = Math.Abs(end.X - start.X);
        var h = Math.Abs(end.Y - start.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
