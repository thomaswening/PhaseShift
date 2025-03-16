using NSubstitute;

using NUnit.Framework;

namespace PhaseShift.Core.Tests;

[TestFixture]
internal class AsyncStopwatchTests
{
    private Action<TimeSpan>? _tickHandler;
    private AsyncStopwatch? _asyncStopwatch;

    [SetUp]
    public void SetUp()
    {
        _tickHandler = Substitute.For<Action<TimeSpan>>();
        _asyncStopwatch = new AsyncStopwatch(_tickHandler);
    }

    [Test]
    public async Task Start_ShouldInvokeAsyncTickHandler()
    {
        // Arrange
        var elapsedTime = TimeSpan.Zero;
        _tickHandler!.Invoke(Arg.Any<TimeSpan>());

        // Act
        _asyncStopwatch!.Start();
        await Task.Delay(50); // Allow some time for the async handler to be invoked
        _asyncStopwatch.Stop();

        // Assert
        _tickHandler.Received().Invoke(Arg.Any<TimeSpan>());
    }

    [Test]
    public void Stop_ShouldStopTheStopwatch()
    {
        // Act
        _asyncStopwatch!.Start();
        _asyncStopwatch.Stop();

        // Assert
        Assert.That(_asyncStopwatch.IsRunning, Is.False);
    }

    [Test]
    public void Reset_ShouldResetElapsedTime()
    {
        // Act
        _asyncStopwatch!.Start();
        Task.Delay(50).Wait(); // Allow some time to pass
        _asyncStopwatch.Reset();

        // Assert
        Assert.That(_asyncStopwatch.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Reset_ShouldInvokeAsyncTickHandlerWithZeroElapsedTime()
    {
        // Arrange
        _tickHandler!.Invoke(Arg.Any<TimeSpan>());

        // Act
        _asyncStopwatch!.Start();
        Task.Delay(50).Wait(); // Allow some time to pass
        _asyncStopwatch.Reset();

        // Assert
        _tickHandler.Received().Invoke(TimeSpan.Zero);
    }

    [Test]
    public void IsRunning_ShouldBeTrueWhenStarted()
    {
        // Act
        _asyncStopwatch!.Start();

        // Assert
        Assert.That(_asyncStopwatch.IsRunning, Is.True);
    }

    [Test]
    public void IsRunning_ShouldBeFalseWhenStopped()
    {
        // Act
        _asyncStopwatch!.Start();
        _asyncStopwatch.Stop();

        // Assert
        Assert.That(_asyncStopwatch.IsRunning, Is.False);
    }

    [Test]
    public async Task Stopwatch_ShouldHaveAcceptableAccuracy()
    {
        // Arrange
        var acceptableDeviation = TimeSpan.FromMilliseconds(1000); // deviation <= 3.6 s per hour
        var expectedElapsedTime = TimeSpan.FromMilliseconds(10_000);

        // Act
        _asyncStopwatch!.Start();
        await Task.Delay(expectedElapsedTime);
        _asyncStopwatch.Stop();
        var actualElapsedTime = _asyncStopwatch.ElapsedTime;

        // Assert
        Assert.That(actualElapsedTime, Is.EqualTo(expectedElapsedTime).Within(acceptableDeviation));
    }
}
