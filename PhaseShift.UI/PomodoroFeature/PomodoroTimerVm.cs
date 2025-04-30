using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using PhaseShift.Core;

using PhaseShift.UI.Common;

namespace PhaseShift.UI.PomodoroFeature;
internal partial class PomodoroTimerVm : PageViewModel
{
    private readonly IDispatcher _dispatcher;
    private readonly PomodoroTimer _pomodoroTimer;

    [ObservableProperty]
    private TimeSpan _elapsedTime;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTimerCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private double _progressCurrentPhase;

    [ObservableProperty]
    private TimeSpan _remainingTime;

    [ObservableProperty]
    private TimeSpan _totalElapsedTime;

    [ObservableProperty]
    private int _totalWorkUnits;

    [ObservableProperty]
    private int _workUnitsBeforeLongBreak;

    [ObservableProperty]
    private int _workUnitsCompleted;

    [ObservableProperty]
    private PomodoroPhase _currentPhase;

    [ObservableProperty]
    private bool _shortBreakEqualsLongBreak;

    [ObservableProperty]
    private TimeSpan _totalTimerDuration;

    [ObservableProperty]
    private TimeSpan _totalRemainingTime;


    public PomodoroTimerVm() : this(null, null) { }

    public PomodoroTimerVm(PomodoroSettings? settings = null, IDispatcher? dispatcher = null)
    {
        // For designer, use a default dispatcher
        _dispatcher = dispatcher ?? new DispatcherWrapper(System.Windows.Application.Current.Dispatcher);

        settings ??= new PomodoroSettings();
        _pomodoroTimer = new PomodoroTimer(() => _dispatcher.Invoke(UpdateTimerState), settings);
        _pomodoroTimer.PhaseCompleted += OnPhaseCompleted;

        CurrentPhase = _pomodoroTimer.Info.CurrentPhase;
        WorkUnitsCompleted = _pomodoroTimer.Info.WorkUnitsCompleted;
        TotalTimerDuration = _pomodoroTimer.Info.TotalTimerDuration;
        UpdateTimerState();

        TotalWorkUnits = _pomodoroTimer.Info.Settings.TotalWorkUnits;
        WorkUnitsBeforeLongBreak = _pomodoroTimer.Info.Settings.WorkUnitsBeforeLongBreak;
        ShortBreakEqualsLongBreak = _pomodoroTimer.Info.Settings.LongBreakDurationSeconds == _pomodoroTimer.Info.Settings.ShortBreakDurationSeconds;
    }

    public event EventHandler<PomodoroTimerCompletedEventArgs>? ActiveTimerCompleted;
    public event EventHandler? PomodoroSettingsRequested;

    public override string Title => "Pomodoro Timer";

    [RelayCommand]
    public void StopTimer()
    {
        _pomodoroTimer.StopActiveTimer();
        IsRunning = _pomodoroTimer.IsRunning;
    }

    private bool CanStartActiveTimer() => !_pomodoroTimer.IsRunning;

    [RelayCommand]
    private void EditPomodoroSettings()
    {
        PomodoroSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnPhaseCompleted(object? sender, bool wasSkipped)
    {
        WorkUnitsCompleted = _pomodoroTimer.Info.WorkUnitsCompleted;
        IsRunning = _pomodoroTimer.IsRunning;

        var oldPhase = CurrentPhase;
        CurrentPhase = _pomodoroTimer.Info.CurrentPhase;

        var args = new PomodoroTimerCompletedEventArgs(oldPhase, CurrentPhase, WorkUnitsCompleted, TotalWorkUnits, wasSkipped);
        ActiveTimerCompleted?.Invoke(this, args);
    }

    [RelayCommand]
    private void ResetCurrentPhase()
    {
        _pomodoroTimer.ResetActiveTimer();
        IsRunning = _pomodoroTimer.IsRunning;
    }

    [RelayCommand]
    private void ResetSession()
    {
        _pomodoroTimer.ResetSession();
        WorkUnitsCompleted = _pomodoroTimer.Info.WorkUnitsCompleted;
        CurrentPhase = _pomodoroTimer.Info.CurrentPhase;
        IsRunning = _pomodoroTimer.IsRunning;

        UpdateTimerState();
    }

    [RelayCommand]
    private void SkipToNextPhase()
    {
        _pomodoroTimer.SkipActiveTimer();
        IsRunning = _pomodoroTimer.IsRunning;
    }

    [RelayCommand(CanExecute = nameof(CanStartActiveTimer))]
    private void StartTimer()
    {
        if (WorkUnitsCompleted >= _pomodoroTimer.Info.Settings.TotalWorkUnits)
        {
            _pomodoroTimer.ResetSession();
        }

        _pomodoroTimer.StartActiveTimer();
        IsRunning = _pomodoroTimer.IsRunning;
    }

    private void UpdateTimerState()
    {
        RemainingTime = _pomodoroTimer.Info.RemainingTimeInCurrentPhase;
        ElapsedTime = _pomodoroTimer.Info.ElapsedTimeInCurrentPhase;
        TotalElapsedTime = _pomodoroTimer.Info.TotalElapsedTime;
        ProgressCurrentPhase = _pomodoroTimer.Info.ProgressCurrentPhase;
        TotalRemainingTime = _pomodoroTimer.Info.TotalRemainingTime;
    }

    public void UpdateSettings(PomodoroSettings settings)
    {
        _pomodoroTimer.UpdateSettings(settings);

        TotalTimerDuration = _pomodoroTimer.Info.TotalTimerDuration;
        ShortBreakEqualsLongBreak = _pomodoroTimer.Info.Settings.LongBreakDurationSeconds == _pomodoroTimer.Info.Settings.ShortBreakDurationSeconds;
        TotalWorkUnits = _pomodoroTimer.Info.Settings.TotalWorkUnits;
        WorkUnitsBeforeLongBreak = _pomodoroTimer.Info.Settings.WorkUnitsBeforeLongBreak;
    }
}