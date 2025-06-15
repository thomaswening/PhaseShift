using NSubstitute;

using NUnit.Framework;

namespace PhaseShift.Core.Tests;

[TestFixture]
internal class AsyncTimerTests
{
    private const int IntervalMilliseconds = 10;
    private const int TestDelayMilliseconds = 1000;

    private const int CountdownDurationMilliseconds = 2000;

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
            TimeSpan.FromMilliseconds(CountdownDurationMilliseconds),
            IntervalMilliseconds);
    }

    [Test]
    public async Task Start_ShouldStartTheTimer()
    {
        // Arrange
        var wasTickCalled = false;
        var isRunning = false;
        var wasCountdownFinishedCalled = false;
        var elapsedTime = TimeSpan.Zero;

        void tickHandler(TimeSpan elapsed)
        {
            wasTickCalled = true;
            elapsedTime = elapsed;
        }

        void countdownFinishedHandler()
        {
            wasCountdownFinishedCalled = true;
        }

        _asyncTimer = new AsyncTimer(
            countdownFinishedHandler, 
            tickHandler, 
            TimeSpan.FromMilliseconds(CountdownDurationMilliseconds), 
            IntervalMilliseconds);

        // Act
        _asyncTimer.Start();
        await Task.Delay(TestDelayMilliseconds);
        isRunning = _asyncTimer.IsRunning;

        _asyncTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isRunning, Is.True,
                "Timer should be running after Start is called.");

            Assert.That(wasTickCalled, Is.True,
                "Tick handler should be called at least once after starting the timer.");

            Assert.That(wasCountdownFinishedCalled, Is.False,
                "Countdown finished handler should not be called unless the countdown completes.");

            Assert.That(elapsedTime.TotalMilliseconds, Is.GreaterThan(IntervalMilliseconds),
                "Elapsed time should be greater than the interval after starting the timer.");
        });
    }

    [Test]
    public async Task Start_ShouldDoNothing_WhenCalledTwice()
    {
        // Act
        _asyncTimer!.Start();
        await Task.Delay(TestDelayMilliseconds);

        _asyncTimer.Start();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncTimer.IsRunning, Is.True,
                "Timer should still be running after calling Start again.");

            Assert.That(_asyncTimer.ElapsedTime, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time should still be greater than zero after calling Start again.");
        });
    }

    [Test]
    public async Task Start_ShouldCompleteTimer_WhenCountdownFinishes()
    {
        // Arrange
        _asyncTimer!.Start();
        await Task.Delay(CountdownDurationMilliseconds + IntervalMilliseconds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncTimer.IsRunning, Is.False,
                "Timer should not be running after the countdown finishes.");

            Assert.That(_asyncTimer.ElapsedTime, Is.EqualTo(TimeSpan.Zero),
                "Elapsed time should be zero after the countdown finishes.");

            _countdownFinishedCallback!.Received(1).Invoke();
        });
    }

    [Test]
    public async Task Stop_ShouldStopTheTimer()
    {
        // Act
        _asyncTimer!.Start();
        await Task.Delay(TestDelayMilliseconds);

        _asyncTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncTimer.IsRunning, Is.False,
                "Timer should not be running after Stop is called.");

            Assert.That(_asyncTimer.ElapsedTime, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time should be greater than zero after stopping the timer.");
        });
    }

    [Test]
    public async Task Stop_ShoulDoNothing_WhenCalledTwice()
    {
        // Act
        _asyncTimer!.Start();
        await Task.Delay(TestDelayMilliseconds);

        _asyncTimer.Stop();
        await Task.Delay(TestDelayMilliseconds);

        _asyncTimer.Stop(); // Call Stop again

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncTimer.IsRunning, Is.False,
                "Timer should still not be running after calling Stop again.");

            Assert.That(_asyncTimer.ElapsedTime, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time should still be greater than zero after calling Stop again.");
        });
    }

    [Test]
    public async Task Reset_ShouldResetTheTimer()
    {
        // Arrange
        _asyncTimer!.Start();
        await Task.Delay(TestDelayMilliseconds);

        _asyncTimer.Reset();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncTimer.IsRunning, Is.False,
                "Timer should not be running after Reset is called.");

            Assert.That(_asyncTimer.ElapsedTime, Is.EqualTo(TimeSpan.Zero),
                "Elapsed time should be zero after Reset is called.");

            Assert.That(_asyncTimer.RemainingTime, Is.EqualTo(_asyncTimer.Duration),
                "Remaining time should equal the initial duration after Reset is called.");
        });
    }

    [Test]
    public void SetDuration_ShouldUpdateTheDuration()
    {
        // Arrange
        var newDuration = TimeSpan.FromSeconds(500);
        _asyncTimer!.Duration = newDuration;

        // Act
        var remainingTime = _asyncTimer.RemainingTime;

        // Assert
        Assert.That(remainingTime, Is.EqualTo(newDuration),
            "Remaining time should equal the new duration after SetDuration is called.");
    }

    [Test]
    public async Task SetDuration_ShouldCompleteTimer_WhenNewDurationLessThanElapsedTime()
    {
        // Arrange
        var newDuration = TimeSpan.FromMilliseconds(CountdownDurationMilliseconds / 2);
        var wasCountdownFinishedCalled = false;

        void countdownFinishedHandler()
        {
            wasCountdownFinishedCalled = true;
        }

        _asyncTimer = new AsyncTimer(
            countdownFinishedHandler,
            _tickCallback!,
            TimeSpan.FromMilliseconds(CountdownDurationMilliseconds),
            IntervalMilliseconds);

        // Act
        _asyncTimer.Start();
        await Task.Delay(CountdownDurationMilliseconds / 2 + TestDelayMilliseconds);
        _asyncTimer.Duration = newDuration;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncTimer.IsRunning, Is.False,
                "Timer should not be running after setting a new duration that is less than elapsed time.");

            Assert.That(_asyncTimer.ElapsedTime, Is.EqualTo(TimeSpan.Zero),
                "Elapsed time should be reset to zero after setting a new duration that is less than elapsed time.");

            Assert.That(wasCountdownFinishedCalled, Is.True,
                "Countdown finished handler should be called when the new duration is less than elapsed time.");
        });
    }

    [Test, CancelAfter(60_000)]
    public void AsyncTimer_ShouldBeThreadSafe_WhenUnderStress(CancellationToken token)
    {
        var successfulIterations = 0;
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        try
        {
            Parallel.For(0, 1_000_000, parallelOptions, i =>
            {
                switch (i % 3)
                {
                    case 0:
                        _asyncTimer!.Start(token);
                        break;
                    case 1:
                        _asyncTimer!.Stop(token);
                        break;
                    case 2:
                        _asyncTimer!.Reset(token);
                        break;
                }

                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref successfulIterations);
            });
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"{nameof(AsyncTimer)} operations timed out after {successfulIterations} successful iterations under stress.");
        }
        catch (Exception ex)
        {
            Assert.Fail($"{nameof(AsyncTimer)} operations failed after {successfulIterations} successful iterations under stress: {ex.Message}");
        }

        Assert.Pass($"{nameof(AsyncTimer)} operations completed under stress.");
    }
}
