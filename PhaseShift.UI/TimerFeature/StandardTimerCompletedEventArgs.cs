using PhaseShift.UI.Common;

namespace PhaseShift.UI.TimerFeature;

internal class StandardTimerCompletedEventArgs(string timerTitle, TimeSpan timerDuration)
    : TimerCompletedEventArgs("Timer Completed")
{
    public string TimerTitle { get; } = timerTitle;
    public TimeSpan TimerDuration { get; } = timerDuration;
}
