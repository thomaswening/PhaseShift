using NUnit.Framework;

using PhaseShift.UI.Common;
using PhaseShift.UI.StopwatchFeature;
using PhaseShift.UI.Tests.Mocks;

namespace PhaseShift.UI.Tests.StopwatchFeature;

[TestFixture]
internal class StopwatchVmTests
{
    private StopwatchVm? _stopwatchVm;
    private IDispatcher? _mockDispatcher;

    [SetUp]
    public void SetUp()
    {
        _mockDispatcher = new MockDispatcher();
        _stopwatchVm = new StopwatchVm(_mockDispatcher);
    }

    [Test]
    public void Constructor_InitializesElapsedTimeToZero()
    {
        Assert.That(_stopwatchVm!.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Constructor_InitializesIsRunningToFalse()
    {
        Assert.That(_stopwatchVm!.IsRunning, Is.False);
    }

    [Test]
    public void StartStopwatch_SetsIsRunningToTrue()
    {
        _stopwatchVm!.StartStopwatchCommand.Execute(null);
        var isRunningAfterStarting = _stopwatchVm.IsRunning;

        Assert.That(isRunningAfterStarting, Is.True);
    }

    [Test]
    public void StartStopwatch_CanExecuteIsFalse_AfterStartingStopwatch()
    {
        _stopwatchVm!.StartStopwatchCommand.Execute(null);
        var canStartAfterStarting = _stopwatchVm.StartStopwatchCommand.CanExecute(null);

        Assert.That(canStartAfterStarting, Is.False);
    }

    [Test]
    public async Task StartStopwatch_IncreasesElapsedTime()
    {
        _stopwatchVm!.StartStopwatchCommand.Execute(null);
        await Task.Delay(1000); // Allow some time for the async handler to be invoked

        Assert.That(_stopwatchVm.ElapsedTime, Is.GreaterThan(TimeSpan.Zero));
    }

    [Test]
    public void PauseStopwatch_SetsIsRunningToFalse()
    {
        _stopwatchVm!.StartStopwatchCommand.Execute(null);
        _stopwatchVm.PauseStopwatchCommand.Execute(null);
        Assert.That(_stopwatchVm.IsRunning, Is.False);
    }

    [Test]
    public async Task PauseStopwatch_DoesNotSetElapsedTimeToZero()
    {
        _stopwatchVm!.StartStopwatchCommand.Execute(null);

        await Task.Delay(1000); // Allow some time for the async handler to be invoked
        _stopwatchVm.PauseStopwatchCommand.Execute(null);

        Assert.That(_stopwatchVm.ElapsedTime, Is.Not.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void ResetStopwatch_SetsIsRunningToFalse()
    {
        _stopwatchVm!.StartStopwatchCommand.Execute(null);
        _stopwatchVm.ResetStopwatchCommand.Execute(null);

        Assert.That(_stopwatchVm.IsRunning, Is.False);
    }

    [Test]
    public void ResetStopwatch_SetsElapsedTimeToZero()
    {
        _stopwatchVm!.StartStopwatchCommand.Execute(null);
        Task.Delay(1000).Wait(); // Allow some time to pass
        _stopwatchVm.ResetStopwatchCommand.Execute(null);

        Assert.That(_stopwatchVm.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public async Task StopwatchVm_ShouldHaveAcceptableAccuracy()
    {
        // Arrange
        var acceptableDeviation = TimeSpan.FromMilliseconds(1000); // deviation <= 3.6 s per hour
        var expectedElapsedTime = TimeSpan.FromMilliseconds(10_000);

        // Act
        _stopwatchVm!.StartStopwatchCommand.Execute(null);
        await Task.Delay(expectedElapsedTime);
        _stopwatchVm.PauseStopwatchCommand.Execute(null);

        // Assert
        Assert.That(_stopwatchVm.ElapsedTime, Is.EqualTo(expectedElapsedTime).Within(acceptableDeviation));
    }
}
