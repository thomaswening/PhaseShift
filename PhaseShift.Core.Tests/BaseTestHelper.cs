namespace PhaseShift.Core.Tests;

internal abstract class BaseTestHelper
{
    protected const int LongBreakDurationSeconds = 1;
    protected const int TestDelayMilliseconds = 200;
    protected const int ShortBreakDurationSeconds = 1;
    protected const int TotalWorkUnits = 2;
    protected const int WorkDurationSeconds = 1;
    protected const int WorkUnitsBeforeLongBreak = 1;

    protected static int SessionDuration => GetTotalTimerDurationSeconds();

    protected static int GetTotalTimerDurationSeconds()
    {
        var totalWorkUnitDuration = WorkDurationSeconds * TotalWorkUnits;
        var numberOfBreaks = TotalWorkUnits - 1;
        var numberOfLongBreaks = numberOfBreaks / WorkUnitsBeforeLongBreak;
        var numberOfShortBreaks = numberOfBreaks - numberOfLongBreaks;

        var shortBreakDuration = ShortBreakDurationSeconds * numberOfShortBreaks;
        var longBreakDuration = LongBreakDurationSeconds * numberOfLongBreaks;

        return totalWorkUnitDuration + shortBreakDuration + longBreakDuration;
    }
}
