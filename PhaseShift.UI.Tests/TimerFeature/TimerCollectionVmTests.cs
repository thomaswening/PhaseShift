using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using PhaseShift.UI.Common;
using PhaseShift.UI.Tests.Mocks;
using PhaseShift.UI.TimerFeature;

namespace PhaseShift.UI.Tests.TimerFeature;

[TestFixture]
internal class TimerCollectionVmTests
{
    private const int TimerDurationMilliseconds = 100;
    private IDispatcher? _dispatcher;
    private TimerCollectionVm? _timerCollectionVm;

    [SetUp]
    public void SetUp()
    {
        _dispatcher = new MockDispatcher();
        _timerCollectionVm = new TimerCollectionVm(_dispatcher)
        {
            NewTimerDuration = TimeSpan.FromMilliseconds(TimerDurationMilliseconds)
        };
    }

    [Test]
    public void Constructor_ShouldAddTimer()
    {
        // Assert
        Assert.That(_timerCollectionVm!.Timers.Count, Is.EqualTo(1));
    }

    [Test]
    public void AddTimer_ShouldAddTimer()
    {
        // Act
        _timerCollectionVm!.AddTimer();

        // Assert
        Assert.That(_timerCollectionVm!.Timers.Count, Is.EqualTo(2));
    }

    [Test]
    public void AddTimer_ShouldAddTimerWithCorrectDuration()
    {
        // Act
        _timerCollectionVm!.AddTimer();

        // Assert
        Assert.That(_timerCollectionVm!.Timers.Last().TimerDuration.TotalMilliseconds, Is.EqualTo(TimerDurationMilliseconds));
    }

    [Test]
    public async Task AddTimer_ShouldSubscribeToTimerCompletedEvent()
    {
        // Arrange
        _timerCollectionVm!.AddTimer();
        var timer = _timerCollectionVm!.Timers.Last();
        var timerCompletedEventRaised = false;
        timer.TimerCompleted += (_, _) => timerCompletedEventRaised = true;

        // Act
        timer.StartTimerCommand.Execute(null);
        await Task.Delay(TimerDurationMilliseconds + 100);

        // Assert
        Assert.That(timerCompletedEventRaised, Is.True);
    }

    [Test]
    public void AddTimer_ShouldSubscribeToDeleteTimerRequestedEvent()
    {
        // Arrange
        _timerCollectionVm!.AddTimer();
        var timer = _timerCollectionVm!.Timers.Last();
        var deleteTimerRequestedEventRaised = false;
        timer.DeleteTimerRequested += (_, _) => deleteTimerRequestedEventRaised = true;

        // Act
        timer.DeleteTimerCommand.Execute(null);

        // Assert
        Assert.That(deleteTimerRequestedEventRaised, Is.True);
    }

    [Test]
    public void ActiveTimersCount_ShouldBeZeroWhenNoTimersAreRunning()
    {
        // Assert
        Assert.That(_timerCollectionVm!.ActiveTimersCount, Is.Zero);
    }

    [Test]
    public void ActiveTimersCount_ShouldBeOneWhenOneTimerIsRunning()
    {
        // Arrange
        _timerCollectionVm!.AddTimer();
        _timerCollectionVm!.Timers.First().StartTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerCollectionVm!.ActiveTimersCount, Is.EqualTo(1));
    }

    [Test]
    public void NextDueTimer_ShouldBeNullWhenNoTimersAreRunning()
    {
        // Assert
        Assert.That(_timerCollectionVm!.NextDueTimer, Is.Null);
    }

    [Test]
    public void NextDueTimer_ShouldBeTheRunningTimerWithTheShortestRemainingTime()
    {
        // Arrange
        _timerCollectionVm!.AddTimer(); // 10 s
        _timerCollectionVm.AddTimerCommand.Execute(null); // 100 ms

        var timer1 = _timerCollectionVm!.Timers.First();
        var timer2 = _timerCollectionVm.Timers.Last();

        timer1.StartTimerCommand.Execute(null);
        timer2.StartTimerCommand.Execute(null);

        // Assert
        Assert.That(_timerCollectionVm!.NextDueTimer, Is.EqualTo(timer2));
    }
}
