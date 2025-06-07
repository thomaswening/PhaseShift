using System.Diagnostics;

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
    [NotifyCanExecuteChangedFor(nameof(StartTimerCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private PomodoroPhase _currentPhase;

    [ObservableProperty]
    private double _progressInCurrentPhase;

    [ObservableProperty]
    private TimeSpan _remainingTimeInCurrentPhase;

    [ObservableProperty]
    private TimeSpan _elapsedTimeInCurrentPhase;

    [ObservableProperty]
    private TimeSpan _elapsedTimeInSession;

    [ObservableProperty]
    private int _totalWorkUnits;

    [ObservableProperty]
    private int _workUnitsBeforeLongBreak;

    [ObservableProperty]
    private int _workUnitsCompleted;

    [ObservableProperty]
    private bool _shortBreakEqualsLongBreak;

    [ObservableProperty]
    private TimeSpan _sessionDuration;

    [ObservableProperty]
    private TimeSpan _remainingTimeInSession;


    public PomodoroTimerVm() : this(null, null) { }

    public PomodoroTimerVm(PomodoroSettings? settings = null, IDispatcher? dispatcher = null)
    {
        // For designer, use a default dispatcher
        _dispatcher = dispatcher ?? new DispatcherWrapper(System.Windows.Application.Current.Dispatcher);

        settings ??= new PomodoroSettings();
        _pomodoroTimer = new PomodoroTimer(_ => _dispatcher.Invoke(UpdateTimerState), settings);
        _pomodoroTimer.PhaseCompleted += OnPhaseCompleted;
        _pomodoroTimer.SessionCompleted += OnSessionCompleted;

        CurrentPhase = _pomodoroTimer.CurrentPhase;
        WorkUnitsCompleted = _pomodoroTimer.CompletedWorkUnits;
        SessionDuration = _pomodoroTimer.SessionDuration;
        UpdateTimerState();

        TotalWorkUnits = _pomodoroTimer.Settings.TotalWorkUnits;
        WorkUnitsBeforeLongBreak = _pomodoroTimer.Settings.WorkUnitsBeforeLongBreak;
        ShortBreakEqualsLongBreak = _pomodoroTimer.Settings.LongBreakDurationSeconds == _pomodoroTimer.Settings.ShortBreakDurationSeconds;
    }

    public event EventHandler<PomodoroPhaseCompletedEventArgs>? PomodoroPhaseCompleted;
    public event EventHandler? PomodoroSettingsRequested;

    public override string Title => "Pomodoro Timer";

    [RelayCommand]
    public void StopTimer()
    {
        _pomodoroTimer.Stop();
        IsRunning = _pomodoroTimer.IsRunning;
    }

    private bool CanStartActiveTimer() => !_pomodoroTimer.IsRunning;

    [RelayCommand]
    private void EditPomodoroSettings()
    {
        PomodoroSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnPhaseCompleted(object? sender, EventArgs e) => NotifyPomodoroPhaseCompleted();
    private void OnSessionCompleted(object? sender, EventArgs e)
    {
        ElapsedTimeInCurrentPhase = TimeSpan.FromSeconds(_pomodoroTimer.Settings.WorkDurationSeconds);
        RemainingTimeInCurrentPhase = TimeSpan.Zero;
        ElapsedTimeInSession = _pomodoroTimer.SessionDuration;
        RemainingTimeInSession = TimeSpan.Zero;

        NotifyPomodoroPhaseCompleted();
    }

    private void NotifyPomodoroPhaseCompleted()
    {
        var args = new PomodoroPhaseCompletedEventArgs(CurrentPhase, WorkUnitsCompleted, TotalWorkUnits);
        PomodoroPhaseCompleted?.Invoke(this, args);
    }


    [RelayCommand]
    private void ResetCurrentPhase()
    {
        _pomodoroTimer.ResetCurrentPhase();
        IsRunning = _pomodoroTimer.IsRunning;

        UpdateTimerState();
    }

    [RelayCommand]
    private void ResetSession()
    {
        _pomodoroTimer.ResetSession();
        WorkUnitsCompleted = _pomodoroTimer.CompletedWorkUnits;
        CurrentPhase = _pomodoroTimer.CurrentPhase;
        IsRunning = _pomodoroTimer.IsRunning;

        UpdateTimerState();
    }

    [RelayCommand]
    private void SkipToNextPhase()
    {
        _pomodoroTimer.SkipToNextPhase();
        WorkUnitsCompleted = _pomodoroTimer.CompletedWorkUnits;
        CurrentPhase = _pomodoroTimer.CurrentPhase;

        UpdateTimerState();
    }

    [RelayCommand(CanExecute = nameof(CanStartActiveTimer))]
    private void StartTimer()
    {
        _pomodoroTimer.Start();
        IsRunning = _pomodoroTimer.IsRunning;
    }

    private void UpdateTimerState()
    {
        ElapsedTimeInCurrentPhase = _pomodoroTimer.ElapsedTimeInCurrentPhase;
        RemainingTimeInCurrentPhase = _pomodoroTimer.RemainingTimeInCurrentPhase;
        ElapsedTimeInSession = _pomodoroTimer.ElapsedTimeInSession;
        RemainingTimeInSession = _pomodoroTimer.RemainingTimeInSession;
        ProgressInCurrentPhase = _pomodoroTimer.ProgressInCurrentPhase;
        IsRunning = _pomodoroTimer.IsRunning;
    }

    public void UpdateSettings(PomodoroSettings settings)
    {

    }
}