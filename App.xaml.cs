using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace NetworkSpeedMonitor;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "debug.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        // UI 线程未处理异常
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 非 UI 线程未处理异常（后台线程）
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        // 未观察的 Task 异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);
    }

    private static void LogException(string source, Exception ex)
    {
        try
        {
            var msg = $"[{DateTime.Now:HH:mm:ss}] [{source}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n";
            File.AppendAllText(LogPath, msg);
        }
        catch
        {
            // 日志写入失败时静默忽略
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("UI线程", e.Exception);
        e.Handled = true; // 阻止应用直接退出，让用户有机会看到问题
        Shutdown();
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogException("后台线程", ex);
        else
            LogException("后台线程", new InvalidOperationException(e.ExceptionObject?.ToString()));
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("Task", e.Exception);
        e.SetObserved(); // 标记为已处理，防止进程终止
    }
}
