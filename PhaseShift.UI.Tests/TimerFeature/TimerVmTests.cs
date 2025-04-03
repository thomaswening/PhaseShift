using NUnit.Framework;

using PhaseShift.UI.Common;
using PhaseShift.UI.Tests.Mocks;
using PhaseShift.UI.TimerFeature;

namespace PhaseShift.UI.Tests.TimerFeature;

[TestFixture]
internal class TimerVmTests
{
    private const int TimerDurationMilliseconds = 1000;
    private IDispatcher? _dispatcher;
    private TimerVm? _timerVm;

    [SetUp]
    public void SetUp()
    {
        _dispatcher = new MockDispatcher();
        _timerVm = new TimerVm(TimeSpan.FromMilliseconds(TimerDurationMilliseconds), _dispatcher);
    }

    [Test]
    public void Constructor_ShouldSetTimerDuration()
    {
        // Assert
        Assert.That(_timerVm!.TimerDuration.TotalMilliseconds, Is.EqualTo(TimerDurationMilliseconds).Within(0));
    }

    [Test]
    public void Constructor_ShouldSetTimerTitle()
    {
        // Assert
        Assert.That(_timerVm!.TimerTitle, Is.EqualTo("1 s timer"));
    }

    [Test]
    public void Constructor_ShouldSetRemainingTime()
    {
        // Assert
        Assert.That(_timerVm!.RemainingTime.TotalMilliseconds, Is.EqualTo(TimerDurationMilliseconds).Within(0));
    }

    [Test]
    public void StartTimerCommand_ShouldSetIsRunningToTrue()
    {
        // Act
        _timerVm!.StartTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerVm.IsRunning, Is.True);
    }

    [Test]
    public void StartTimerCommand_ShouldNotBeAbleToExecute_WhenTimerIsRunning()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);
        // Act
        var canExecute = _timerVm.StartTimerCommand.CanExecute(null);
        // Assert
        Assert.That(canExecute, Is.False);
    }

    [Test]
    public void StartTimerCommand_ShouldBeAbleToExecute_WhenTimerIsNotRunning()
    {
        // Act
        var canExecute = _timerVm!.StartTimerCommand.CanExecute(null);
        // Assert
        Assert.That(canExecute, Is.True);
    }

    [Test]
    public async Task RemainingTime_ShouldDecrease_WhenTimerIsRunning()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);

        // Act
        await Task.Delay(100);

        // Assert
        Assert.That(_timerVm.RemainingTime.TotalMilliseconds, Is.LessThan(TimerDurationMilliseconds));
    }

    [Test]
    public async Task RemainingTime_ShouldNotChange_WhenTimerIsNotRunning()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(100);
        _timerVm.StopTimerCommand.Execute(null);
        var remainingTimeBefore = _timerVm.RemainingTime;

        // Act
        await Task.Delay(100);

        // Assert
        Assert.That(_timerVm.RemainingTime, Is.EqualTo(remainingTimeBefore));
    }

    [Test]
    public async Task RemainingTime_ShouldBeTimerDuration_WhenTimerIsFinished()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);

        // Act
        await Task.Delay(TimerDurationMilliseconds + 100);

        // Assert
        Assert.That(_timerVm.RemainingTime.TotalMilliseconds, Is.EqualTo(TimerDurationMilliseconds));
    }

    [Test]
    public async Task RemainingTime_ShouldBeAlmostTimerDuration_WhenTimerIsAlmostFinished()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);

        // Act
        await Task.Delay(TimerDurationMilliseconds - 100);

        // Assert
        Assert.That(_timerVm.RemainingTime.TotalMilliseconds, Is.EqualTo(100).Within(20));
    }

    [Test]
    public async Task RemainingTime_ShouldBeTimerDuration_WhenTimerIsReset()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);

        // Act
        await Task.Delay(100);
        _timerVm.ResetTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerVm.RemainingTime.TotalMilliseconds, Is.EqualTo(TimerDurationMilliseconds));
    }

    [Test]
    public async Task Progress_ShouldIncrease_WhenTimerIsRunning()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);

        // Act
        await Task.Delay(100);

        // Assert
        Assert.That(_timerVm.Progress, Is.GreaterThan(0));
    }

    [Test]
    public async Task Progress_ShouldNotChange_WhenTimerIsNotRunning()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(100);
        _timerVm.StopTimerCommand.Execute(null);
        var progressBefore = _timerVm.Progress;

        // Act
        await Task.Delay(100);

        // Assert
        Assert.That(_timerVm.Progress, Is.EqualTo(progressBefore));
    }

    [Test]
    public async Task Progress_ShouldBeCloseToOne_WhenTimerIsAlmostFinished()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);

        // Act
        await Task.Delay(TimerDurationMilliseconds - 100);

        // Assert
        Assert.That(_timerVm.Progress, Is.EqualTo(1).Within(0.1));
    }

    [Test]
    public async Task Progress_ShouldBeZero_WhenTimerIsFinished()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);

        // Act
        await Task.Delay(TimerDurationMilliseconds);

        // Assert
        Assert.That(_timerVm.Progress, Is.EqualTo(0));
    }

    [Test]
    public void StopTimerCommand_ShouldSetIsRunningToFalse()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);

        // Act
        _timerVm.StopTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerVm.IsRunning, Is.False);
    }

    [Test]
    public async Task StopTimerCommand_ShouldNotResetRemainingTime()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(100); // Let the timer run for a while

        // Act
        _timerVm.StopTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerVm.RemainingTime.TotalMilliseconds, Is.LessThan(TimerDurationMilliseconds));
    }

    [Test]
    public async Task StopTimerCommand_ShouldNotResetProgress()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(100); // Let the timer run for a while

        // Act
        _timerVm.StopTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerVm.Progress, Is.GreaterThan(0));
    }

    [Test]
    public async Task ResetTimerCommand_ShouldResetRemainingTime()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(100); // Let the timer run for a while

        // Act
        _timerVm.ResetTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerVm.RemainingTime.TotalMilliseconds, Is.EqualTo(TimerDurationMilliseconds));
    }

    [Test]
    public async Task ResetTimerCommand_ShouldResetProgress()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(100); // Let the timer run for a while

        // Act
        _timerVm.ResetTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerVm.Progress, Is.EqualTo(0));
    }

    [Test]
    public async Task ResetTimerCommand_ShouldSetIsRunningToFalse()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(100); // Let the timer run for a while

        // Act
        _timerVm.ResetTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerVm.IsRunning, Is.False);
    }


    [Test]
    public void DeleteTimerCommand_ShouldInvokeDeleteTimerRequestedEvent()
    {
        // Arrange
        bool eventInvoked = false;
        _timerVm!.DeleteTimerRequested += (sender, args) => eventInvoked = true;

        // Act
        _timerVm.DeleteTimerCommand.Execute(null);

        // Assert
        Assert.That(eventInvoked, Is.True);
    }

    [Test]
    public void DeleteTimerCommand_ShouldStopTimer()
    {
        // Arrange
        _timerVm!.StartTimerCommand.Execute(null);

        // Act
        _timerVm.DeleteTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerVm.IsRunning, Is.False);
    }

    [Test]
    public async Task TimerCompleted_ShouldBeInvoked_AfterTimerWasCompleted()
    {
        // Arrange
        bool eventInvoked = false;
        _timerVm!.TimerCompleted += (sender, args) => eventInvoked = true;

        // Act
        _timerVm.StartTimerCommand.Execute(null);
        await Task.Delay(TimerDurationMilliseconds + 100);

        // Assert
        Assert.That(eventInvoked, Is.True);
    }
}
