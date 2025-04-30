namespace PhaseShift.Core;

public class PomodoroSessionManager
{
    private readonly Action _tickCallback;
    private readonly Dictionary<PomodoroPhase, AsyncTimer> _timers;
    public PomodoroSessionManager(Action tickCallback, PomodoroSettings settings)
    {
        ValidateSettings(settings);
        Settings = settings;
        _timers = InitializeTimers();
        ActiveTimer = _timers[PomodoroPhase.Work]; // start with work unit timer
        _tickCallback = tickCallback;
    }

    /// <summary>
    /// Occurs when a phase is completed.
    /// A boolean value is passed to indicate if the phase was skipped or normally completed.
    /// </summary>
    public event EventHandler<bool>? PhaseCompleted;

    public event EventHandler? SessionCompleted;

    public AsyncTimer ActiveTimer { get; private set; }
    public PomodoroPhase CurrentPhase { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsRunning { get; private set; }
    public PomodoroSettings Settings { get; private set; }
    public int WorkUnitsCompleted { get; private set; }

    public void ResetActiveTimer()
    {
        ActiveTimer.Reset();
        IsRunning = false;
    }

    public void ResetSession()
    {
        ActiveTimer.Reset();
        SetNextPhase(PomodoroPhase.Work);

        WorkUnitsCompleted = 0;
        IsRunning = false;
    }

    public void SkipActiveTimer()
    {
        ActiveTimer.Reset();
        OnActiveTimerCompleted(wasSkipped: true);
    }

    public void StartActiveTimer()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;

        Task.Run(ActiveTimer.Start);
    }

    public void StopActiveTimer()
    {
        ActiveTimer.Stop();
        IsRunning = false;
    }

    public void UpdateSettings(PomodoroSettings settings)
    {
        var wasRunning = IsRunning;
        if (IsRunning)
        {
            StopActiveTimer();
        }

        ValidateSettings(settings);

        Settings = settings;

        SetTimerDuration(PomodoroPhase.Work, TimeSpan.FromSeconds(settings.WorkDurationSeconds));
        SetTimerDuration(PomodoroPhase.ShortBreak, TimeSpan.FromSeconds(settings.ShortBreakDurationSeconds));
        SetTimerDuration(PomodoroPhase.LongBreak, TimeSpan.FromSeconds(settings.LongBreakDurationSeconds));
        Settings.WorkUnitsBeforeLongBreak = settings.WorkUnitsBeforeLongBreak;

        SetTotalWorkUnits(settings.TotalWorkUnits);

        _tickCallback(); // propagate changes directly to the UI / subscribers

        if (wasRunning)
        {
            StartActiveTimer();
        }
    }

    private static void ValidateSettings(PomodoroSettings settings)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.WorkUnitsBeforeLongBreak, 1, nameof(settings.WorkUnitsBeforeLongBreak));
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.TotalWorkUnits, 1, nameof(settings.TotalWorkUnits));
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.WorkDurationSeconds, 1, nameof(settings.WorkDurationSeconds));
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.ShortBreakDurationSeconds, 1, nameof(settings.ShortBreakDurationSeconds));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.ShortBreakDurationSeconds, settings.LongBreakDurationSeconds, nameof(settings.ShortBreakDurationSeconds));
    }

    private AsyncTimer GetTimer(int duration)
    {
        return new AsyncTimer(() => OnActiveTimerCompleted(), _ => _tickCallback(), TimeSpan.FromSeconds(duration));
    }

    private Dictionary<PomodoroPhase, AsyncTimer> InitializeTimers()
    {
        var workUnitTimer = GetTimer(Settings.WorkDurationSeconds);
        var shortBreakTimer = GetTimer(Settings.ShortBreakDurationSeconds);
        var longBreakTimer = GetTimer(Settings.LongBreakDurationSeconds);

        return new Dictionary<PomodoroPhase, AsyncTimer>
                {
                    { PomodoroPhase.Work, workUnitTimer },
                    { PomodoroPhase.ShortBreak, shortBreakTimer },
                    { PomodoroPhase.LongBreak, longBreakTimer }
                };
    }

    private void OnActiveTimerCompleted(bool wasSkipped = false)
    {
        if (CurrentPhase is PomodoroPhase.Work
            && Settings.TotalWorkUnits - 1 <= WorkUnitsCompleted) // -1 because the just completed work unit has not yet been counted
        {
            WorkUnitsCompleted++;
            IsCompleted = true;
            PhaseCompleted?.Invoke(this, wasSkipped);
            SessionCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (CurrentPhase is PomodoroPhase.Work)
        {
            WorkUnitsCompleted++;
            SetBreakTimer();
        }
        else
        {
            SetNextPhase(PomodoroPhase.Work);
        }

        PhaseCompleted?.Invoke(this, wasSkipped);

        if (IsRunning && !IsCompleted)
        {
            ActiveTimer.Start();
        }
    }

    private void SetBreakTimer()
    {
        if (WorkUnitsCompleted % Settings.WorkUnitsBeforeLongBreak == 0)
        {
            SetNextPhase(PomodoroPhase.LongBreak);
        }
        else
        {
            SetNextPhase(PomodoroPhase.ShortBreak);
        }
    }

    private void SetNextPhase(PomodoroPhase nextPhase)
    {
        CurrentPhase = nextPhase;
        ActiveTimer = _timers[nextPhase];
    }

    private void SetTimerDuration(PomodoroPhase phase, TimeSpan duration)
    {
        if (_timers[phase].Duration == duration)
        {
            return;
        }

        _timers[phase].Duration = duration;

        // Advance to the next phase if the current phase's duration is changed to less than the already elapsed time.
        if (CurrentPhase == phase && duration <= ActiveTimer.ElapsedTime)
        {
            StopActiveTimer();
            OnActiveTimerCompleted();
        }        
    }

    private void SetTotalWorkUnits(int newTotalWorkUnits)
    {
        if (newTotalWorkUnits == Settings.TotalWorkUnits)
        {
            return;
        }

        if (newTotalWorkUnits < WorkUnitsCompleted + 1)
        // +1 accounts for the current work phase not yet being completed and counted.
        // If in a break phase, one more work unit must be completed to finish the session.
        // This means: If newTotalWorkUnits is less than the current work units completed + 1,
        // the session is already completed with the new setting.
        {
            StopActiveTimer();

            WorkUnitsCompleted = newTotalWorkUnits;
            SessionCompleted?.Invoke(this, EventArgs.Empty);
        }

        Settings.TotalWorkUnits = newTotalWorkUnits;
    }

}

public enum PomodoroPhase
{
    Work,
    ShortBreak,
    LongBreak
}
