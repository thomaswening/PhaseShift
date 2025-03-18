using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

using PhaseShift.UI.Common;
using PhaseShift.UI.StopwatchFeature;
using PhaseShift.UI.Tests.Mocks;

namespace PhaseShift.Core.AccuracyTestTool.Tests;

internal class StopwatchVmTest(AccuracyTest.Options options) : AccuracyTest(options)
{
    public override string Title => typeof(StopwatchVm).ToString();

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
