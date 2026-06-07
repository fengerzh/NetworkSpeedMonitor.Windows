using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NetworkSpeedMonitor;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

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
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var (dl, ul) = NetworkSpeed.GetSpeed();
        SpeedText.Text = $"⬇{FormatSpeed(dl)}  {FormatSpeed(ul)}⬆";
    }

    private static string FormatSpeed(double mbps)
    {
        if (mbps < 0.01) return "0";
        if (mbps >= 1.0) return $"{mbps:F1}M";
        return $"{mbps * 1000:F0}K";
    }
}
