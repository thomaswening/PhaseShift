using PhaseShift.UI.Tests.Mocks;
using PhaseShift.UI.TimerFeature;

namespace PhaseShift.AccuracyTestTool.Tests;
internal class TimerVmTest(AccuracyTest.Options options) : AccuracyTest(options)
{
    protected override double MeasureElapsedTime()
    {
        var dispatcher = new MockDispatcher();
        var timerVm = new TimerVm(TimeSpan.FromMilliseconds(_expectedElapsedTimeMilliseconds + 100), dispatcher); // Add 100 ms to ensure the timer doesn't finish
        timerVm.StartTimerCommand.Execute(null);
        Thread.Sleep(_expectedElapsedTimeMilliseconds);
        timerVm.StopTimerCommand.Execute(null);

        return _expectedElapsedTimeMilliseconds - (timerVm.RemainingTime.TotalMilliseconds - 100); // deduct 100 ms to account for the extra time added
    }
}
