using System.Windows;
using System.Windows.Input;

namespace EcountManager;

public partial class TransparencyControlWindow : Window
{
    public event EventHandler? ToggleRequested;
    public event EventHandler<WindowMovedEventArgs>? MovedByUser;

    private bool _isPositionedByOwner;
    private double _lastLeft;
    private double _lastTop;

    public TransparencyControlWindow()
    {
        InitializeComponent();
        LocationChanged += TransparencyControlWindow_OnLocationChanged;
    }

    public void SetState(bool isTransparent) => TransparencyToggle.IsChecked = isTransparent;

    public void SetPosition(double left, double top)
    {
        _isPositionedByOwner = true;
        try
        {
            Left = left;
            Top = top;
            _lastLeft = left;
            _lastTop = top;
        }
        finally
        {
            _isPositionedByOwner = false;
        }
    }

    private void Window_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;

        e.Handled = true;
        var startLeft = Left;
        var startTop = Top;

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        var wasDragged = Math.Abs(Left - startLeft) >= SystemParameters.MinimumHorizontalDragDistance ||
                         Math.Abs(Top - startTop) >= SystemParameters.MinimumVerticalDragDistance;
        if (!wasDragged) ToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    private void TransparencyControlWindow_OnLocationChanged(object? sender, EventArgs e)
    {
        var deltaX = Left - _lastLeft;
        var deltaY = Top - _lastTop;
        _lastLeft = Left;
        _lastTop = Top;

        if (_isPositionedByOwner || (Math.Abs(deltaX) < 0.01 && Math.Abs(deltaY) < 0.01)) return;
        MovedByUser?.Invoke(this, new WindowMovedEventArgs(deltaX, deltaY));
    }
}

public sealed class WindowMovedEventArgs(double deltaX, double deltaY) : EventArgs
{
    public double DeltaX { get; } = deltaX;
    public double DeltaY { get; } = deltaY;
}
