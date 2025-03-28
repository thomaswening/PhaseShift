namespace PhaseShift.UI.Common;

internal abstract class TimerCompletedEventArgs(string title) : EventArgs
{
    public string Title { get; } = title;
}
