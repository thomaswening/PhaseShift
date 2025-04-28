using System.Media;
using System.Runtime.InteropServices;
using System.Windows;

using PhaseShift.Core;
using PhaseShift.UI.Common;
using PhaseShift.UI.PomodoroFeature;
using PhaseShift.UI.TimerFeature;

namespace PhaseShift.UI.Notifications;
internal partial class NotificationService
{
    private readonly string _appName;
    private readonly IDispatcher _dispatcher;
    private readonly SystemSound _notificationSound = SystemSounds.Exclamation;
    private readonly NotifyIcon _notifyIcon;

    public NotificationService(IDispatcher dispatcher, string appName, NotifyIcon notifyIcon)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName, nameof(appName));
        _appName = appName;
        _notifyIcon = notifyIcon;
        _dispatcher = dispatcher;
    }

    public static void FlashWindow()
    {
        var flashInfo = new FLASHWINFO()
        {
            cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
            hwnd = new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle,
            dwFlags = 3, // FLASHW_ALL
            uCount = 5, // Flash 5 times
            dwTimeout = 0
        };

        FlashWindowEx(ref flashInfo);
    }

    public static void PullWindowToTheFront()
    {
        var window = System.Windows.Application.Current.MainWindow;
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();
    }

    public void OnTimerCompleted(object? sender, EventArgs e)
    {
        var message = CreateMessageFromEvent(sender, e); // If the event is not recognized, message will be null

        // Don't notify if the timer was skipped and it's not the end of the session
        if (message is null)
        {
            return;
        }

        _dispatcher.Invoke(() =>
        {
            _notificationSound.Play();
            DisplayBalloonTip(message);

            FlashWindow();
            PullWindowToTheFront();
        });
    }

    private static string? CreateMessageFromEvent(object? sender, EventArgs e)
    {
        return e switch
        {
            StandardTimerCompletedEventArgs args => CreateStandardTimerMessage(args),
            PomodoroTimerCompletedEventArgs args => CreatePomodoroMessage(args),
            _ => null,
        };
    }

    private static string? CreatePomodoroMessage(PomodoroTimerCompletedEventArgs args)
    {
        if (args.TimerCompleted)
        {
            return "Pomodoro session completed!";
        }

        var msg = args.NextPhase switch
        {
            PomodoroPhase.Work => "Time to get back to work!",
            PomodoroPhase.ShortBreak => $"Time for a short break!",
            PomodoroPhase.LongBreak => "Time for a long break!",
            _ => null
        };

        if (args.WorkUnitsCompleted < args.TotalWorkUnits)
        {
            msg += $"\nCompleted work units: {args.WorkUnitsCompleted} / {args.TotalWorkUnits}";
        }

        return msg;
    }

    private static string CreateStandardTimerMessage(StandardTimerCompletedEventArgs args)
    {
        var timerTitle = args.TimerTitle;
        var timerDuration = args.TimerDuration;
        return $"Timer '{timerTitle}' completed after {timerDuration:mm\\:ss}";
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FlashWindowEx(ref FLASHWINFO pwfi);

    private void DisplayBalloonTip(string message)
    {
        const int timeout = 5000;
        _notifyIcon.ShowBalloonTip(timeout, _appName, message, ToolTipIcon.Info);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public nint hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }
}
