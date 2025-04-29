using CommunityToolkit.Mvvm.ComponentModel;

using PhaseShift.UI.PomodoroFeature;
using PhaseShift.UI.StopwatchFeature;
using PhaseShift.UI.TimerFeature;

namespace PhaseShift.UI.StatusOverview;

internal partial class StatusVm(
    PomodoroTimerVm pomodoroTimerVm,
    TimerCollectionVm timerCollectionVm,
    StopwatchVm stopwatchVm,
    ObservableObject selectedVm) : ObservableObject
{
    public PomodoroTimerVm PomodoroTimerVm { get; init; } = pomodoroTimerVm;
    public TimerCollectionVm TimerCollectionVm { get; init; } = timerCollectionVm;
    public StopwatchVm StopwatchVm { get; init; } = stopwatchVm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PomodoroVmIsSelected))]
    [NotifyPropertyChangedFor(nameof(TimerCollectionVmIsSelected))]
    [NotifyPropertyChangedFor(nameof(StopwatchVmIsSelected))]
    public ObservableObject _selectedVm = selectedVm;

    public bool PomodoroVmIsSelected => SelectedVm is PomodoroNavigationVm;
    public bool TimerCollectionVmIsSelected => SelectedVm is TimerCollectionVm;
    public bool StopwatchVmIsSelected => SelectedVm is StopwatchVm;
}
