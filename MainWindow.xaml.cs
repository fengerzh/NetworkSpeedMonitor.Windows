using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NetworkSpeedMonitor;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _mouseTrackTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_N = 0x4E;

    private bool _hidden;
    private bool _isFaded;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    private void SetClickThrough(bool passThrough)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = (int)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        if (passThrough)
            exStyle |= WS_EX_TRANSPARENT;
        else
            exStyle &= ~WS_EX_TRANSPARENT;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)exStyle);
    }

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        MouseLeftButtonDown += (s, e) => DragMove();
        _mouseTrackTimer.Tick += OnMouseTrackTick;
        var menu = new System.Windows.Controls.ContextMenu();
        var exit = new System.Windows.Controls.MenuItem { Header = "退出" };
        exit.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exit);
        ContextMenu = menu;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var screen = SystemParameters.WorkArea;
        Left = screen.Right - Width - 10;
        Top = screen.Top + 10;

        _timer.Tick += OnTick;
        _timer.Start();
        OnTick(this, EventArgs.Empty);

        // 注册全局快捷键 Ctrl+Alt+N
        var hwnd = new WindowInteropHelper(this).Handle;
        RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_N);

        // 监听窗口消息
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            ToggleVisibility();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ToggleVisibility()
    {
        _hidden = !_hidden;

        // 快捷键切换时重置淡出状态
        if (_isFaded)
        {
            _mouseTrackTimer.Stop();
            _isFaded = false;
            SetClickThrough(false);
            AnimateOpacity(1.0);
        }

        MainBorder.Visibility = _hidden ? Visibility.Collapsed : Visibility.Visible;
    }

    protected override void OnClosed(EventArgs e)
    {
        _mouseTrackTimer.Stop();
        var hwnd = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(hwnd, HOTKEY_ID);
        base.OnClosed(e);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var (dl, ul) = NetworkSpeed.GetSpeed();
        SpeedText.Text = $"⬇{FormatSpeed(dl)}  {FormatSpeed(ul)}⬆";
    }

    private void Border_MouseEnter(object sender, MouseEventArgs e)
    {
        _isFaded = true;
        AnimateOpacity(0.05);
        SetClickThrough(true); // Win32 级别穿透
        _mouseTrackTimer.Start();
    }

    private void Border_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isFaded) return; // 已经淡出并设置了穿透，由定时器负责恢复
        AnimateOpacity(1.0);
    }

    private void OnMouseTrackTick(object? sender, EventArgs e)
    {
        if (IsMouseWithinWindow()) return;

        _mouseTrackTimer.Stop();
        _isFaded = false;
        SetClickThrough(false);
        AnimateOpacity(1.0);
    }

    private bool IsMouseWithinWindow()
    {
        if (!GetCursorPos(out var pt)) return false;
        var pos = PointFromScreen(new Point(pt.X, pt.Y));
        return pos.X >= 0 && pos.X <= ActualWidth && pos.Y >= 0 && pos.Y <= ActualHeight;
    }

    private void AnimateOpacity(double to)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        MainBorder.BeginAnimation(OpacityProperty, animation);
    }

    private static string FormatSpeed(double mbps)
    {
        if (mbps < 0.01) return "0";
        if (mbps >= 1.0) return $"{mbps:F1}M";
        return $"{mbps * 1000:F0}K";
    }
}
