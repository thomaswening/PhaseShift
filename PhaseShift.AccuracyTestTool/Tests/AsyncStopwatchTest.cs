using PhaseShift.Core;

namespace PhaseShift.AccuracyTestTool.Tests;

internal class AsyncStopwatchTest(AccuracyTest.Options options, string? title = null) : AccuracyTest(options, title)
{
    protected override double MeasureElapsedTime()
    {
        var asyncStopwatch = new AsyncStopwatch(_ => { }, 10);
        asyncStopwatch.Start();
        Thread.Sleep(_expectedElapsedTimeMilliseconds);
        asyncStopwatch.Stop();

        return asyncStopwatch.ElapsedTime.TotalMilliseconds;
    }
}