namespace PhaseShift.Core;

public class PomodoroSettings
{
    public int LongBreakDurationSeconds { get; set; } = 900; // 15 minutes
    public int ShortBreakDurationSeconds { get; set; } = 300; // 5 minutes
    public int TotalWorkUnits { get; set; } = 12;
    public int WorkDurationSeconds { get; set; } = 1500; // 25 minutes
    public int WorkUnitsBeforeLongBreak { get; set; } = 4;
}
