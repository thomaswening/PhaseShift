namespace PhaseShift.Core.AccuracyTestTool.Tests;

internal class AsyncStopwatchTest(AccuracyTest.Options options) : AccuracyTest(options)
{
    public override string Title => typeof(AsyncStopwatch).ToString();

    protected override double MeasureElapsedTime()
    {
        var asyncStopwatch = new AsyncStopwatch(_ => { }, 10);
        asyncStopwatch.Start();
        Thread.Sleep(_expectedElapsedTimeMilliseconds);
        asyncStopwatch.Stop();

        return asyncStopwatch.ElapsedTime.TotalMilliseconds;
    }
}