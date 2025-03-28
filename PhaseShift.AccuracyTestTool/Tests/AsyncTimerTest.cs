using PhaseShift.Core;

namespace PhaseShift.AccuracyTestTool.Tests;
internal class AsyncTimerTest(AccuracyTest.Options options) : AccuracyTest(options)
{
    protected override double MeasureElapsedTime()
    {
        var timer = new AsyncTimer(
            () => { },
            _ => { },
            TimeSpan.FromMilliseconds(_expectedElapsedTimeMilliseconds + 100)); // Add 100ms to ensure the timer has not finished

        timer.Start();
        Thread.Sleep(_expectedElapsedTimeMilliseconds); // Wait 100 ms less than timer duration, so it does not finish
        timer.Stop();

        return timer.ElapsedTime.TotalMilliseconds;
    }
}
