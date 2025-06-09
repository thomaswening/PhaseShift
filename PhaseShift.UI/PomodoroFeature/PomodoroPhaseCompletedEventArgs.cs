using PhaseShift.Core;
using PhaseShift.UI.Common;

namespace PhaseShift.UI.PomodoroFeature;

internal class PomodoroPhaseCompletedEventArgs(
    PomodoroPhase newPhase,
    int workUnitsCompleted,
    int totalWorkUnits) : TimerCompletedEventArgs("Pomodoro Timer Completed")
{
    public PomodoroPhase NewPhase { get; } = newPhase;
    public int WorkUnitsCompleted { get; } = workUnitsCompleted;
    public int TotalWorkUnits { get; } = totalWorkUnits;
    public bool TimerCompleted => WorkUnitsCompleted == TotalWorkUnits;
}