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

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_N = 0x4E;

    private bool _hidden;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        MouseLeftButtonDown += (s, e) => DragMove();
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
        if (_hidden)
        {
            MainBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            MainBorder.Visibility = Visibility.Visible;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(hwnd, HOTKEY_ID);
        base.OnClosed(e);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var (dl, ul) = NetworkSpeed.GetSpeed();
        SpeedText.Text = $"⬇{FormatSpeed(dl)}  {FormatSpeed(ul)}⬆";
    }

    private void Border_MouseEnter(object sender, MouseEventArgs e) => AnimateOpacity(0.05);

    private void Border_MouseLeave(object sender, MouseEventArgs e) => AnimateOpacity(1.0);

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
