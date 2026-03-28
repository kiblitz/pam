using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pam.Views;

public partial class RegionSelector : Window
{
    private Point _startPoint;
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
        _startPoint = PointToScreen(e.GetPosition(this));
        _isDragging = true;
        SelectionRect.Visibility = Visibility.Visible;
        InstructionText.Visibility = Visibility.Collapsed;
        Mouse.Capture(this);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var current = PointToScreen(e.GetPosition(this));
        var x = Math.Min(_startPoint.X, current.X);
        var y = Math.Min(_startPoint.Y, current.Y);
        var w = Math.Abs(current.X - _startPoint.X);
        var h = Math.Abs(current.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        Mouse.Capture(null);

        var endPoint = PointToScreen(e.GetPosition(this));
        var x = Math.Min(_startPoint.X, endPoint.X);
        var y = Math.Min(_startPoint.Y, endPoint.Y);
        var w = Math.Abs(endPoint.X - _startPoint.X);
        var h = Math.Abs(endPoint.Y - _startPoint.Y);

        if (w > 5 && h > 5)
        {
            SelectedRegion = new Rect(x, y, w, h);
            RegionSelected = true;
            DialogResult = true;
            Close();
        }
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
