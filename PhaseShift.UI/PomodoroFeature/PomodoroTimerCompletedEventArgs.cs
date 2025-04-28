using PhaseShift.Core;
using PhaseShift.UI.Common;

namespace PhaseShift.UI.PomodoroFeature;

internal class PomodoroTimerCompletedEventArgs(
    PomodoroPhase completedPhase,
    PomodoroPhase nextPhase,
    int workUnitsCompleted,
    int totalWorkUnits,
    bool wasSkipped = false) : TimerCompletedEventArgs("Pomodoro Timer Completed")
{
    public PomodoroPhase CompletedPhase { get; } = completedPhase;
    public int WorkUnitsCompleted { get; } = workUnitsCompleted;
    public int TotalWorkUnits { get; } = totalWorkUnits;
    public PomodoroPhase NextPhase { get; } = nextPhase;
    public bool TimerCompleted => WorkUnitsCompleted == TotalWorkUnits;
    public bool WasSkipped { get; } = wasSkipped;
}
