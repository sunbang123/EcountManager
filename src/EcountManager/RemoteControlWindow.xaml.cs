using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace EcountManager;

public partial class RemoteControlWindow : Window
{
    private const int WmHotKey = 0x0312;
    private const int ManagerHotKeyId = 0xEC01;
    private const int MemoHotKeyId = 0xEC02;
    private const int TransparencyHotKeyId = 0xEC03;
    private const uint ModControl = 0x0002;
    private const uint ModNoRepeat = 0x4000;
    private const uint Key1 = 0x31;
    private const uint Key2 = 0x32;
    private const uint Key3 = 0x33;
    private const double TransparentOpacity = 0;

    private MainWindow? _managerWindow;
    private MemoWidgetWindow? _memoWindow;
    private TransparencyControlWindow? _transparencyControlWindow;
    private HwndSource? _windowSource;
    private IntPtr _windowHandle;
    private bool _isTransparentMode;
    private bool _isFollowingTransparencyControl;

    public RemoteControlWindow()
    {
        InitializeComponent();
        Loaded += RemoteControlWindow_OnLoaded;
        LocationChanged += RemoteControlWindow_OnLocationChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        _windowSource = HwndSource.FromHwnd(_windowHandle);
        _windowSource.AddHook(WindowMessageHook);

        var managerRegistered = RegisterHotKey(_windowHandle, ManagerHotKeyId, ModControl | ModNoRepeat, Key1);
        var memoRegistered = RegisterHotKey(_windowHandle, MemoHotKeyId, ModControl | ModNoRepeat, Key2);
        var transparencyRegistered = RegisterHotKey(_windowHandle, TransparencyHotKeyId, ModControl | ModNoRepeat, Key3);
        if (!managerRegistered || !memoRegistered || !transparencyRegistered)
            ShortcutHint.Text = "단축키 등록 실패 · 다른 앱의 Ctrl+1/2/3 사용 여부를 확인하세요";
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, ManagerHotKeyId);
            UnregisterHotKey(_windowHandle, MemoHotKeyId);
            UnregisterHotKey(_windowHandle, TransparencyHotKeyId);
        }
        if (_transparencyControlWindow is not null)
        {
            _transparencyControlWindow.ToggleRequested -= TransparencyControlWindow_OnToggleRequested;
            _transparencyControlWindow.MovedByUser -= TransparencyControlWindow_OnMovedByUser;
        }
        _windowSource?.RemoveHook(WindowMessageHook);
        base.OnClosed(e);
    }

    private void RemoteControlWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_transparencyControlWindow is not null) return;

        _transparencyControlWindow = new TransparencyControlWindow
        {
            Owner = this
        };
        _transparencyControlWindow.ToggleRequested += TransparencyControlWindow_OnToggleRequested;
        _transparencyControlWindow.MovedByUser += TransparencyControlWindow_OnMovedByUser;
        PositionTransparencyControl();
        _transparencyControlWindow.Show();
    }

    private void RemoteControlWindow_OnLocationChanged(object? sender, EventArgs e) => PositionTransparencyControl();

    private void PositionTransparencyControl()
    {
        if (_transparencyControlWindow is null || _isFollowingTransparencyControl) return;
        _transparencyControlWindow.SetPosition(
            Left + (ActualWidth - _transparencyControlWindow.Width) / 2,
            Top + ActualHeight + 8);
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void TransparencyControlWindow_OnToggleRequested(object? sender, EventArgs e) => ToggleTransparency();

    private void TransparencyControlWindow_OnMovedByUser(object? sender, WindowMovedEventArgs e)
    {
        _isFollowingTransparencyControl = true;
        try
        {
            Left += e.DeltaX;
            Top += e.DeltaY;
        }
        finally
        {
            _isFollowingTransparencyControl = false;
        }
    }

    private void ToggleManager_OnClick(object sender, RoutedEventArgs e) => ToggleManager();

    private void ToggleManager()
    {
        if (_managerWindow is null)
        {
            _managerWindow = new MainWindow();
            _managerWindow.Closed += ManagerWindow_OnClosed;
            _managerWindow.IsVisibleChanged += ChildWindow_OnIsVisibleChanged;
            ApplyCurrentOpacity(_managerWindow);
            _managerWindow.ShowActivated = !_isTransparentMode;
            _managerWindow.Show();
        }
        else if (_managerWindow.IsVisible)
        {
            _managerWindow.Hide();
        }
        else
        {
            ApplyCurrentOpacity(_managerWindow);
            _managerWindow.ShowActivated = !_isTransparentMode;
            _managerWindow.Show();
            if (_managerWindow.WindowState == WindowState.Minimized) _managerWindow.WindowState = WindowState.Normal;
            _managerWindow.Activate();
        }

        UpdateButtonStates();
    }

    private void ToggleMemo_OnClick(object sender, RoutedEventArgs e) => ToggleMemo();

    private void ToggleMemo()
    {
        if (_memoWindow is null)
        {
            _memoWindow = new MemoWidgetWindow();
            _memoWindow.Closed += MemoWindow_OnClosed;
            _memoWindow.IsVisibleChanged += ChildWindow_OnIsVisibleChanged;
            ApplyCurrentOpacity(_memoWindow);
            _memoWindow.Topmost = !_isTransparentMode;
            _memoWindow.ShowActivated = !_isTransparentMode;
            _memoWindow.Show();
        }
        else if (_memoWindow.IsVisible)
        {
            _memoWindow.Hide();
        }
        else
        {
            ApplyCurrentOpacity(_memoWindow);
            _memoWindow.Topmost = !_isTransparentMode;
            _memoWindow.ShowActivated = !_isTransparentMode;
            _memoWindow.Show();
            if (!_isTransparentMode) _memoWindow.Activate();
        }

        UpdateButtonStates();
    }

    private void ManagerWindow_OnClosed(object? sender, EventArgs e)
    {
        if (_managerWindow is not null)
        {
            _managerWindow.Closed -= ManagerWindow_OnClosed;
            _managerWindow.IsVisibleChanged -= ChildWindow_OnIsVisibleChanged;
        }
        _managerWindow = null;
        UpdateButtonStates();
    }

    private void MemoWindow_OnClosed(object? sender, EventArgs e)
    {
        if (_memoWindow is not null)
        {
            _memoWindow.Closed -= MemoWindow_OnClosed;
            _memoWindow.IsVisibleChanged -= ChildWindow_OnIsVisibleChanged;
        }
        _memoWindow = null;
        UpdateButtonStates();
    }

    private void ChildWindow_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateButtonStates();

    private void UpdateButtonStates()
    {
        var managerOn = _managerWindow?.IsVisible == true;
        ManagerStatus.Text = managerOn ? "켜짐 · 클릭해서 숨기기" : "꺼짐 · 클릭해서 켜기";
        var memoOn = _memoWindow?.IsVisible == true;
        MemoStatus.Text = memoOn ? "켜짐 · 클릭해서 숨기기" : "꺼짐 · 클릭해서 켜기";
        ManagerButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(managerOn ? "#EAF0FF" : "#FFFFFF"));
        MemoButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(memoOn ? "#FFF3DD" : "#FFFFFF"));
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmHotKey) return IntPtr.Zero;

        switch (wParam.ToInt32())
        {
            case ManagerHotKeyId:
                ToggleManager();
                handled = true;
                break;
            case MemoHotKeyId:
                ToggleMemo();
                handled = true;
                break;
            case TransparencyHotKeyId:
                ToggleTransparency();
                handled = true;
                break;
        }

        return IntPtr.Zero;
    }

    private void ToggleTransparency()
    {
        _isTransparentMode = !_isTransparentMode;
        _transparencyControlWindow?.SetState(_isTransparentMode);
        Opacity = _isTransparentMode ? TransparentOpacity : 1;
        IsHitTestVisible = !_isTransparentMode;
        Topmost = true;
        ShowActivated = !_isTransparentMode;

        if (_managerWindow is not null)
        {
            ApplyCurrentOpacity(_managerWindow);
            _managerWindow.ShowActivated = !_isTransparentMode;
        }
        if (_memoWindow is not null)
        {
            ApplyCurrentOpacity(_memoWindow);
            _memoWindow.Topmost = !_isTransparentMode;
            _memoWindow.ShowActivated = !_isTransparentMode;
        }

        ShortcutHint.Text = _isTransparentMode
            ? "투명 모드 · Ctrl+3으로 원래 화면 복원"
            : "Ctrl+1 Ecount · Ctrl+2 메모 · Ctrl+3 투명";
    }

    private void ApplyCurrentOpacity(Window window)
    {
        window.Opacity = _isTransparentMode ? TransparentOpacity : 1;
        window.IsHitTestVisible = !_isTransparentMode;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);

    private void Exit_OnClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
