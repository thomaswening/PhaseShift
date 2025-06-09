using NSubstitute;

using NUnit.Framework;

namespace PhaseShift.Core.Tests;

[TestFixture]
internal class AsyncStopwatchTests
{
    private const int DefaultIntervalMilliseconds = 10;
    private const int TestDelayMilliseconds = 5 * DefaultIntervalMilliseconds;

    private Action<TimeSpan>? _tickHandler;
    private AsyncStopwatch? _asyncStopwatch;

    [SetUp]
    public void SetUp()
    {
        _tickHandler = Substitute.For<Action<TimeSpan>>();
        _asyncStopwatch = new AsyncStopwatch(_tickHandler, DefaultIntervalMilliseconds);
    }

    [Test]
    public async Task Start_ShouldStartTheStopwatch()
    {
        // Arrange
        var wasTickHandlerCalled = false;
        var elapsedTime = TimeSpan.Zero;

        void tickHandler(TimeSpan elapsed)
        {
            wasTickHandlerCalled = true;
            elapsedTime = elapsed;
        }

        _asyncStopwatch = new AsyncStopwatch(tickHandler, DefaultIntervalMilliseconds);

        // Act
        _asyncStopwatch.Start();
        await Task.Delay(100 * DefaultIntervalMilliseconds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncStopwatch.IsRunning, Is.True,
                "Stopwatch should be running after Start is called.");

            Assert.That(wasTickHandlerCalled, Is.True,
                "Tick handler should be called at least once after starting the stopwatch.");

            Assert.That(elapsedTime.TotalMilliseconds, Is.GreaterThan(DefaultIntervalMilliseconds),
                "Elapsed time should be greater than the interval after starting the stopwatch.");
        });
    }

    [Test]
    public async Task Start_ShouldDoNothing_WhenCalledTwice()
    {
        // Act
        _asyncStopwatch!.Start();
        await Task.Delay(TestDelayMilliseconds);

        _asyncStopwatch.Start(); // Call Start again
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        Assert.That(_asyncStopwatch.IsRunning, Is.True,
            "Stopwatch should still be running after Start is called twice.");
    }

    [Test]
    public async Task Stop_ShouldStopTheStopwatch()
    {
        // Act
        _asyncStopwatch!.Start();
        await Task.Delay(TestDelayMilliseconds);

        _asyncStopwatch.Stop();
        var elapsedTimeAfterStop = _asyncStopwatch.ElapsedTime;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncStopwatch.IsRunning, Is.False,
                "Stopwatch should not be running after Stop is called.");

            Assert.That(elapsedTimeAfterStop.TotalMilliseconds, Is.GreaterThan(0),
                "Elapsed time should be greater than zero after stopping the stopwatch.");
        });
    }

    [Test]
    public async Task Stop_ShouldDoNothing_WhenCalledTwice()
    {
        // Arrange
        var elapsedTimeAfterFirstStop = TimeSpan.Zero;
        var elapsedTimeAfterSecondStop = TimeSpan.Zero;

        // Act
        _asyncStopwatch!.Start();
        await Task.Delay(TestDelayMilliseconds);

        _asyncStopwatch.Stop();
        elapsedTimeAfterFirstStop = _asyncStopwatch.ElapsedTime;
        await Task.Delay(TestDelayMilliseconds);

        _asyncStopwatch.Stop(); // Call Stop again
        elapsedTimeAfterSecondStop = _asyncStopwatch.ElapsedTime;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncStopwatch.IsRunning, Is.False,
                "Stopwatch should not be running after Stop is called twice.");

            Assert.That(elapsedTimeAfterSecondStop, Is.EqualTo(elapsedTimeAfterFirstStop),
                "Elapsed time should not change after calling Stop a second time.");
        });
    }

    [Test]
    public void Reset_ShouldResetTheStopwatchAndInvokeTickHandler()
    {
        // Arrange
        var wasTickHandlerCalled = false;
        var elapsedTime = TimeSpan.Zero;

        void tickHandler(TimeSpan elapsed)
        {
            wasTickHandlerCalled = true;
            elapsedTime = elapsed;
        }

        _asyncStopwatch = new AsyncStopwatch(tickHandler, DefaultIntervalMilliseconds);

        // Act
        _asyncStopwatch.Start();
        _asyncStopwatch.Reset();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_asyncStopwatch.IsRunning, Is.False,
                "Stopwatch should not be running after Reset is called.");

            Assert.That(_asyncStopwatch.ElapsedTime, Is.EqualTo(TimeSpan.Zero),
                "Elapsed time should be zero after Reset is called.");

            Assert.That(wasTickHandlerCalled, Is.True,
                "Tick handler should be called with zero elapsed time after Reset is called.");
        });
    }

    [Test, CancelAfter(60_000)]
    public void AsyncStopwatch_ShouldBeThreadSafe_WhenUnderStress(CancellationToken token)
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
                        _asyncStopwatch!.Start(token);
                        break;
                    case 1:
                        _asyncStopwatch!.Stop(token);
                        break;
                    case 2:
                        _asyncStopwatch!.Reset(token);
                        break;
                }

                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref successfulIterations);
            });
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"{nameof(AsyncStopwatch)} operations timed out after {successfulIterations} successful iterations under stress.");
        }
        catch (Exception ex)
        {
            Assert.Fail($"{nameof(AsyncStopwatch)} operations failed after {successfulIterations} successful iterations under stress: {ex.Message}");
        }

        Assert.Pass($"{nameof(AsyncStopwatch)} operations completed under stress.");
    }
}