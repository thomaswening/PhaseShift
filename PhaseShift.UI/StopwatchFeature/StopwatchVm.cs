using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using PhaseShift.Core;
using PhaseShift.UI.Common;

namespace PhaseShift.UI.StopwatchFeature;

internal partial class StopwatchVm : PageViewModel
{
    public override string Title => "Stopwatch";

    private readonly AsyncStopwatch _timer;
    private readonly IDispatcher _dispatcher;

    [ObservableProperty]
    private TimeSpan elapsedTime = TimeSpan.Zero;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartStopwatchCommand))]
    private bool isRunning = false;

    public StopwatchVm() : this(new DispatcherWrapper(System.Windows.Application.Current.Dispatcher)) { }

    public StopwatchVm(IDispatcher dispatcher)
    {
        _timer = new AsyncStopwatch(UpdateElapsedTime);
        _dispatcher = dispatcher;
    }

    [RelayCommand(CanExecute = nameof(CanStartStopwatch))]
    private void StartStopwatch()
    {
        IsRunning = true;
        _timer.Start();
    }

    private bool CanStartStopwatch() => !IsRunning;

    private void UpdateElapsedTime(TimeSpan span)
    {
        _dispatcher.Invoke(() => ElapsedTime = span);
    }

    [RelayCommand]
    private void PauseStopwatch()
    {
        _timer.Stop();
        IsRunning = false;
    }

    [RelayCommand]
    private void ResetStopwatch()
    {
        _timer.Reset();
        IsRunning = false;
    }
}