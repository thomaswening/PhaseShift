namespace PhaseShift.Core;

public class PomodoroTimer
{
    private readonly Dictionary<PomodoroPhase, AsyncTimer> _timers;
    private AsyncTimer _currentTimer;
    private TimeSpan _elapsedTimeInPreviousPhases = TimeSpan.Zero;
    private bool IsCompleted => CompletedWorkUnits >= Settings.TotalWorkUnits;

    public event EventHandler? SessionCompleted;
    public event EventHandler? PhaseCompleted;
    public event EventHandler? PhaseSkipped;

    public PomodoroSettings Settings { get; private set; }
    public TimeSpan ElapsedTimeInCurrentPhase => _currentTimer.ElapsedTime;
    public TimeSpan ElapsedTimeInSession => _elapsedTimeInPreviousPhases + ElapsedTimeInCurrentPhase;
    public TimeSpan RemainingTimeInCurrentPhase => _currentTimer.RemainingTime;
    public TimeSpan SessionDuration => GetSessionDuration();
    public TimeSpan RemainingTimeInSession => GetRemainingTimeInSession();
    public double ProgressInCurrentPhase => _currentTimer.Progress;
    public PomodoroPhase CurrentPhase => _timers.FirstOrDefault(x => x.Value == _currentTimer).Key;
    public bool IsRunning => _currentTimer.IsRunning;
    public int CompletedWorkUnits { get; private set; }

    public PomodoroTimer(Action<TimeSpan> tickCallback) : this(tickCallback, new PomodoroSettings()) { }

    public PomodoroTimer(Action<TimeSpan> tickCallback, PomodoroSettings settings)
    {
        ValidateSettings(settings);

        Settings = settings;
        _timers = InitializeTimers(tickCallback);
        _currentTimer = _timers[PomodoroPhase.Work]; // Start with the work phase
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        if (IsCompleted)
        {
            ResetSession();
        }

        _currentTimer.Start();
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _currentTimer.Stop();
    }

    public void ResetSession()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Reset();
        }

        if (CurrentPhase is not PomodoroPhase.Work)
        {
            _currentTimer = _timers[PomodoroPhase.Work];
        }

        _elapsedTimeInPreviousPhases = TimeSpan.Zero;
        CompletedWorkUnits = 0;
    }

    public void ResetCurrentPhase()
    {
        if (IsCompleted)
        {
            _elapsedTimeInPreviousPhases -= _currentTimer.Duration;
            --CompletedWorkUnits;
        }

        _currentTimer.Reset();
    }

    public void SkipToNextPhase()
    {
        if (IsCompleted)
        {
            return;
        }

        var wasRunning = IsRunning;
        if (IsRunning)
        {
            _currentTimer.Stop();
        }

        AdvancePhase(skipped: true, wasRunning);
    }

    public void UpdateSettings(PomodoroSettings newSettings)
    {
        ValidateSettings(newSettings);
        UpdateTotalWorkUnits(newSettings);
        UpdatePhaseDurations(newSettings);
        UpdateWorkUnitsBeforeLongBreak(newSettings);
    }

    private void UpdateWorkUnitsBeforeLongBreak(PomodoroSettings newSettings)
    {
        if (Settings.WorkUnitsBeforeLongBreak == newSettings.WorkUnitsBeforeLongBreak)
        {
            return;
        }

        Settings.WorkUnitsBeforeLongBreak = newSettings.WorkUnitsBeforeLongBreak;
    }

    private void UpdatePhaseDurations(PomodoroSettings newSettings)
    {
        foreach ((var phase, var timer) in _timers)
        {
            var newDuration = phase switch
            {
                PomodoroPhase.Work => TimeSpan.FromSeconds(newSettings.WorkDurationSeconds),
                PomodoroPhase.ShortBreak => TimeSpan.FromSeconds(newSettings.ShortBreakDurationSeconds),
                PomodoroPhase.LongBreak => TimeSpan.FromSeconds(newSettings.LongBreakDurationSeconds),
                _ => throw new InvalidOperationException("Unknown Pomodoro phase")
            };

            UpdateTimerDuration(phase, timer, newDuration);
        }

        Settings.WorkDurationSeconds = newSettings.WorkDurationSeconds;
        Settings.ShortBreakDurationSeconds = newSettings.ShortBreakDurationSeconds;
        Settings.LongBreakDurationSeconds = newSettings.LongBreakDurationSeconds;
    }

    private void UpdateTimerDuration(PomodoroPhase phase, AsyncTimer timer, TimeSpan newDuration)
    {
        if (timer.Duration == newDuration)
        {
            return;
        }

        var wasRunning = timer.IsRunning;
        if (timer.IsRunning)
        {
            timer.Stop();
        }

        timer.Duration = newDuration;

        if (CurrentPhase == phase && newDuration <= timer.ElapsedTime)
        {
            // Complete the current phase and move to the next one
            timer.Reset();
            AdvancePhase(skipped: false, wasRunning);
            return;
        }

        if (wasRunning)
        {
            timer.Start();
        }
    }

    private void UpdateTotalWorkUnits(PomodoroSettings newSettings)
    {
        if (CurrentPhase is PomodoroPhase.Work && newSettings.TotalWorkUnits < CompletedWorkUnits + 1
            || CurrentPhase is not PomodoroPhase.Work && newSettings.TotalWorkUnits <= CompletedWorkUnits)
        {
            _currentTimer.Reset();
            // reset elapsed time in previous phases to zero,
            // then use GetSessionDuration to get new session duration
            // and then set elapsed time to that value to mark the session as completed

            Settings.TotalWorkUnits = newSettings.TotalWorkUnits;
            _elapsedTimeInPreviousPhases = GetSessionDuration();

            SessionCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }
        else
        {
            Settings.TotalWorkUnits = newSettings.TotalWorkUnits;
        }
    }

    private TimeSpan GetSessionDuration()
    {
        var numberOfBreaks = Settings.TotalWorkUnits - 1;
        var numberOfLongBreaks = numberOfBreaks / Settings.WorkUnitsBeforeLongBreak;
        var numberOfShortBreaks = numberOfBreaks - numberOfLongBreaks;

        var workSeconds = Settings.WorkDurationSeconds * Settings.TotalWorkUnits;
        var longBreakSeconds = Settings.LongBreakDurationSeconds * numberOfLongBreaks;
        var shortBreakSeconds = Settings.ShortBreakDurationSeconds * numberOfShortBreaks;

        return TimeSpan.FromSeconds(workSeconds + longBreakSeconds + shortBreakSeconds);
    }

    private TimeSpan GetRemainingTimeInSession()
    {
        var remainingSessionTime = SessionDuration - ElapsedTimeInSession;
        if (remainingSessionTime < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }
        
        return SessionDuration - ElapsedTimeInSession;
    }

    private AsyncTimer GetNextPhaseTimer()
    {
        if (CurrentPhase is not PomodoroPhase.Work)
        {
            return _timers[PomodoroPhase.Work];
        }

        if (CompletedWorkUnits % Settings.WorkUnitsBeforeLongBreak == 0)
        {
            return _timers[PomodoroPhase.LongBreak];
        }

        return _timers[PomodoroPhase.ShortBreak];
    }

    private static void ValidateSettings(PomodoroSettings settings)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.LongBreakDurationSeconds, 1, nameof(settings.LongBreakDurationSeconds));
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.ShortBreakDurationSeconds, 1, nameof(settings.ShortBreakDurationSeconds));
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.WorkDurationSeconds, 1, nameof(settings.WorkDurationSeconds));

        ArgumentOutOfRangeException.ThrowIfLessThan(settings.TotalWorkUnits, 1, nameof(settings.TotalWorkUnits));
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.WorkUnitsBeforeLongBreak, 1, nameof(settings.WorkUnitsBeforeLongBreak));

        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.ShortBreakDurationSeconds, settings.LongBreakDurationSeconds, nameof(settings.ShortBreakDurationSeconds));
    }

    private Dictionary<PomodoroPhase, AsyncTimer> InitializeTimers(Action<TimeSpan> tickCallback)
    {
        return new Dictionary<PomodoroPhase, AsyncTimer>
        {
            { PomodoroPhase.Work, new AsyncTimer(OnPhaseTimerCompleted, tickCallback, TimeSpan.FromSeconds(Settings.WorkDurationSeconds)) },
            { PomodoroPhase.ShortBreak, new AsyncTimer(OnPhaseTimerCompleted, tickCallback, TimeSpan.FromSeconds(Settings.ShortBreakDurationSeconds)) },
            { PomodoroPhase.LongBreak, new AsyncTimer(OnPhaseTimerCompleted, tickCallback, TimeSpan.FromSeconds(Settings.LongBreakDurationSeconds)) }
        };
    }

    private void OnPhaseTimerCompleted()
    {
        AdvancePhase(skipped: false, wasRunning: true);
    }

    private void AdvancePhase(bool skipped, bool wasRunning)
    {
        // add the duration because in the passed time should match the session's duration
        _elapsedTimeInPreviousPhases += _currentTimer.Duration;

        if (CurrentPhase == PomodoroPhase.Work)
        {
            ++CompletedWorkUnits;

            if (IsCompleted)
            {
                _currentTimer.Reset();
                SessionCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        _currentTimer.Reset();
        _currentTimer = GetNextPhaseTimer();

        // notify before starting the next phase timer
        if (skipped)
        {
            PhaseSkipped?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            PhaseCompleted?.Invoke(this, EventArgs.Empty);
        }

        if (wasRunning)
        {
            _currentTimer.Start();
        }
    }
}

public enum PomodoroPhase
{
    Work,
    ShortBreak,
    LongBreak
}