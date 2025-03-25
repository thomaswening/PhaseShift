using NSubstitute;

using NUnit.Framework;

namespace PhaseShift.Core.Tests;

[TestFixture]
internal class AsyncTimerTests
{
    private const int CountdownDurationMilliseconds = 1000;
    private Action? _countdownFinishedCallback;
    private Action<TimeSpan>? _tickCallback;
    private AsyncTimer? _asyncTimer;

    [SetUp]
    public void SetUp()
    {
        _countdownFinishedCallback = Substitute.For<Action>();
        _tickCallback = Substitute.For<Action<TimeSpan>>();

        _asyncTimer = new AsyncTimer(
            _countdownFinishedCallback,
            _tickCallback,
            TimeSpan.FromMilliseconds(CountdownDurationMilliseconds));
    }

    [Test]
    public async Task Start_ShouldInvokeTickCallback()
    {
        // Arrange
        _tickCallback!.Invoke(Arg.Any<TimeSpan>());

        // Act
        _asyncTimer!.Start();
        await Task.Delay(50); // Allow some time for the async handler to be invoked
        _asyncTimer.Stop();

        // Assert
        _tickCallback.Received().Invoke(Arg.Any<TimeSpan>());
    }

    [Test]
    public void Stop_ShouldStopTheTimer()
    {
        // Act
        _asyncTimer!.Start();
        _asyncTimer.Stop();

        // Assert
        Assert.That(_asyncTimer.IsRunning, Is.False);
    }

    [Test]
    public void Reset_ShouldResetElapsedTime()
    {
        // Act
        _asyncTimer!.Start();
        Task.Delay(50).Wait(); // Allow some time to pass
        _asyncTimer.Reset();

        // Assert
        Assert.That(_asyncTimer.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Reset_ShouldInvokeTickCallbackWithZeroElapsedTime()
    {
        // Arrange
        _tickCallback!.Invoke(Arg.Any<TimeSpan>());

        // Act
        _asyncTimer!.Start();
        Task.Delay(50).Wait(); // Allow some time to pass
        _asyncTimer.Reset();

        // Assert
        _tickCallback.Received().Invoke(TimeSpan.Zero);
    }

    [Test]
    public async Task IsRunning_ShouldBeTrueWhenStarted()
    {
        // Act
        _asyncTimer!.Start();
        await Task.Delay(50); // Allow some time for the async handler to be invoked

        // Assert
        Assert.That(_asyncTimer.IsRunning, Is.True);
    }

    [Test]
    public void IsRunning_ShouldBeFalseWhenStopped()
    {
        // Act
        _asyncTimer!.Start();
        _asyncTimer.Stop();

        // Assert
        Assert.That(_asyncTimer.IsRunning, Is.False);
    }

    [Test]
    public async Task Timer_ShouldInvokeCountdownFinishedCallback()
    {
        // Arrange
        _countdownFinishedCallback!.Invoke();

        // Act
        _asyncTimer!.Start();
        await Task.Delay(CountdownDurationMilliseconds + 100); // Allow enough time for the countdown to finish

        // Assert
        _countdownFinishedCallback.Received().Invoke();
    }

    [Test]
    public async Task ElapsedTime_ShouldBeZeroWhenCountdownFinished()
    {
        // Act
        _asyncTimer!.Start();
        await Task.Delay(CountdownDurationMilliseconds + 100); // Allow enough time for the countdown to finish

        // Assert
        Assert.That(_asyncTimer.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public async Task ElapsedTime_ShouldIncrease_AfterStarting()
    {
        // Act
        _asyncTimer!.Start();
        await Task.Delay(50); // Allow some time to pass
        var elapsedTime = _asyncTimer.ElapsedTime;

        // Assert
        Assert.That(elapsedTime, Is.GreaterThan(TimeSpan.Zero));
    }

    [Test]
    public async Task ElapsedTime_ShouldNotBeZero_AfterPausing()
    {
        // Act
        _asyncTimer!.Start();
        await Task.Delay(100); // Allow some time to pass
        _asyncTimer.Stop();
        var elapsedTime = _asyncTimer.ElapsedTime;

        // Assert
        Assert.That(elapsedTime, Is.GreaterThan(TimeSpan.Zero));
    }
}
