using PhaseShift.UI.StopwatchFeature;
using PhaseShift.UI.Tests.Mocks;

namespace PhaseShift.AccuracyTestTool.Tests;

internal class StopwatchVmTest(AccuracyTest.Options options, string? title = null) : AccuracyTest(options, title)
{
    protected override double MeasureElapsedTime()
    {
        var mockDispatcher = new MockDispatcher();
        var stopwatchVm = new StopwatchVm(mockDispatcher);
        stopwatchVm.StartStopwatchCommand.Execute(null);
        Thread.Sleep(_expectedElapsedTimeMilliseconds);
        stopwatchVm.PauseStopwatchCommand.Execute(null);
        Thread.Sleep(100); // Wait for elapsed time to update

        return stopwatchVm.ElapsedTime.TotalMilliseconds;
    }
}
