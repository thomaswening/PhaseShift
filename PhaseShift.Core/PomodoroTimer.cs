namespace PhaseShift.Core;

public class PomodoroTimer
{
    private readonly PomodoroSessionManager _sessionManager;
    private TimeSpan _elapsedTimeInCompletedPhases = TimeSpan.Zero;

    public PomodoroTimer(Action tickCallback, PomodoroSettings? settings = null)
    {
        settings ??= new PomodoroSettings();
        _sessionManager = new PomodoroSessionManager(tickCallback, settings);
        _sessionManager.PhaseCompleted += OnPhaseCompleted;
        _sessionManager.SessionCompleted += OnSessionCompleted;
    }

    /// <summary>
    /// Occurs when a phase is completed.
    /// A boolean value is passed to indicate if the phase was skipped or normally completed.
    /// </summary>
    public event EventHandler<bool>? PhaseCompleted;

    public event EventHandler? SessionCompleted;

    /// <summary>
    /// Gets the timer information such as the current phase, the remaining time in the current phase,
    /// the total elapsed time, and the total remaining time.
    /// </summary>
    public TimerInfo Info => new(this);

    public bool IsRunning => _sessionManager.IsRunning;

    public void ResetActiveTimer()
    {
        if (Info.WorkUnitsCompleted == 0)
        {
            _elapsedTimeInCompletedPhases = TimeSpan.Zero;
        }

        _sessionManager.ResetActiveTimer();
    }

    public void ResetSession()
    {
        _elapsedTimeInCompletedPhases = TimeSpan.Zero;
        _sessionManager.ResetSession();
    }

    public void SkipActiveTimer() => _sessionManager.SkipActiveTimer();

    public void StartActiveTimer()
    {
        if (IsRunning)
        {
            return;
        }

        _sessionManager.StartActiveTimer();

        if (_elapsedTimeInCompletedPhases == TimeSpan.Zero)
        {
            _elapsedTimeInCompletedPhases += TimeSpan.FromSeconds(1);
        }
    }

    public void StopActiveTimer() => _sessionManager.StopActiveTimer();

    public void UpdateSettings(PomodoroSettings settings) => _sessionManager.UpdateSettings(settings);

    private void OnPhaseCompleted(object? sender, bool e) => PhaseCompleted?.Invoke(this, e);

    private void OnSessionCompleted(object? sender, EventArgs e) => SessionCompleted?.Invoke(this, e);

    /// <summary>
    /// Encapsulates the timer information such as the current phase, the remaining time in the current phase,
    /// the total elapsed time, and the total remaining time.
    /// </summary>
    public class TimerInfo(PomodoroTimer pomodoroTimer)
    {
        private readonly PomodoroTimer pomodoroTimer = pomodoroTimer;

        public PomodoroPhase CurrentPhase => pomodoroTimer._sessionManager.CurrentPhase;
        public TimeSpan ElapsedTimeInCurrentPhase => pomodoroTimer._sessionManager.ActiveTimer.ElapsedTime;
        public bool IsRunning => pomodoroTimer._sessionManager.IsRunning;
        public int LongBreakDuration => pomodoroTimer._sessionManager.Settings.LongBreakDurationSeconds;
        public double ProgressCurrentPhase => ElapsedTimeInCurrentPhase.TotalSeconds / pomodoroTimer._sessionManager.ActiveTimer.Duration.TotalSeconds;
        public TimeSpan RemainingTimeInCurrentPhase => pomodoroTimer._sessionManager.ActiveTimer.RemainingTime;
        public PomodoroSettings Settings => pomodoroTimer._sessionManager.Settings;
        public int ShortBreakDuration => pomodoroTimer._sessionManager.Settings.ShortBreakDurationSeconds;
        public TimeSpan TotalElapsedTime => ElapsedTimeInCurrentPhase + pomodoroTimer._elapsedTimeInCompletedPhases;
        public TimeSpan TotalRemainingTime => TotalTimerDuration - TotalElapsedTime;
        public TimeSpan TotalTimerDuration => GetTotalTimerDuration();
        public int TotalWorkUnits => pomodoroTimer._sessionManager.Settings.TotalWorkUnits;
        public int WorkUnitsBeforeLongBreak => pomodoroTimer._sessionManager.Settings.WorkUnitsBeforeLongBreak;
        public int WorkUnitsCompleted => pomodoroTimer._sessionManager.WorkUnitsCompleted;

        private TimeSpan GetTotalTimerDuration()
        {
            // Calculate the remaining time from the projected time with the current settings
            // and add the time from the completed phases.

            // Necessary to use the remaining work units and elapsed time in completed phases
            // because the settings might have been changed during the session.

            var remainingWorkUnits = TotalWorkUnits - WorkUnitsCompleted;
            var remainingWorkUnitDuration = TimeSpan.FromSeconds(remainingWorkUnits * Settings.WorkDurationSeconds);
            var remainingBreaks = remainingWorkUnits - 1;
            var passedLongBreaks = WorkUnitsCompleted / WorkUnitsBeforeLongBreak;

            var longBreaks = TotalWorkUnits / WorkUnitsBeforeLongBreak;
            if (TotalWorkUnits % WorkUnitsBeforeLongBreak == 0) 
            {
                longBreaks--; // Cannot have a break at the end of the session
            }

            var remainingLongBreaks = longBreaks - passedLongBreaks;
            var remainingShortBreaks = remainingBreaks - remainingLongBreaks;

            var remainingShortBreakDuration = TimeSpan.FromSeconds(remainingShortBreaks * ShortBreakDuration);
            var remainingLongBreakDuration = TimeSpan.FromSeconds(remainingLongBreaks * LongBreakDuration);

            return pomodoroTimer._elapsedTimeInCompletedPhases
                + remainingWorkUnitDuration
                + remainingShortBreakDuration
                + remainingLongBreakDuration;
        }
    }
}
