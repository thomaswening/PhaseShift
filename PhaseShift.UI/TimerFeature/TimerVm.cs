using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using PhaseShift.Core;
using PhaseShift.UI.Common;

namespace PhaseShift.UI.TimerFeature;

internal partial class TimerVm : ObservableObject
{
    private readonly AsyncTimer _timer;
    private readonly IDispatcher _dispatcher;

    public event EventHandler<StandardTimerCompletedEventArgs>? TimerCompleted;
    public event EventHandler? DeleteTimerRequested;

    [ObservableProperty]
    private string _timerTitle;

    [ObservableProperty]
    private TimeSpan _timerDuration;

    [ObservableProperty]
    private TimeSpan _remainingTime;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTimerCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private double _progress;

    public TimerVm() : this(TimeSpan.FromSeconds(3), new DispatcherWrapper(System.Windows.Application.Current.Dispatcher)) { } // for designer

    public TimerVm(TimeSpan duration, IDispatcher dispatcher)
    {
        TimerDuration = duration;
        TimerTitle = GetTitleFromDuration();
        RemainingTime = TimerDuration;
        _timer = new AsyncTimer(OnTimerFinished, Update, TimerDuration);
        _dispatcher = dispatcher;
    }

    private void OnTimerFinished()
    {
        _dispatcher.Invoke(() => IsRunning = false);

        var args = new StandardTimerCompletedEventArgs(TimerTitle, TimerDuration);
        TimerCompleted?.Invoke(this, args);
    }

    private string GetTitleFromDuration()
    {
        var parts = new List<string>();
        if (TimerDuration.Hours > 0)
        {
            parts.Add($"{TimerDuration.Hours} h");
        }
        if (TimerDuration.Minutes > 0)
        {
            parts.Add($"{TimerDuration.Minutes} m");
        }
        if (TimerDuration.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{TimerDuration.Seconds} s");
        }
        return string.Join(" ", parts) + " timer";
    }

    [RelayCommand(CanExecute = nameof(CanStartTimer))]
    private void StartTimer()
    {
        IsRunning = true;
        _timer.Start();
    }

    private bool CanStartTimer() => !IsRunning;

    private void Update(TimeSpan span)
    {
        _dispatcher.Invoke(() =>
        {
            RemainingTime = _timer.RemainingTime;
            Progress = _timer.Progress;
        });
    }

    [RelayCommand]
    private void StopTimer()
    {
        _timer.Stop();
        IsRunning = false;
    }

    [RelayCommand]
    private void ResetTimer()
    {
        _timer.Reset();
        IsRunning = false;
    }

    [RelayCommand]
    private void DeleteTimer()
    {
        StopTimer();
        DeleteTimerRequested?.Invoke(this, EventArgs.Empty);
        DeleteTimerRequested = null;
        TimerCompleted = null;
    }
}
