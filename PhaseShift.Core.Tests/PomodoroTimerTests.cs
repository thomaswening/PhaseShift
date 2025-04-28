using NSubstitute;

using NUnit.Framework;

namespace PhaseShift.Core.Tests;

[TestFixture]
internal class PomodoroTimerTests : BaseTestHelper
{
    private PomodoroTimer? _pomodoroTimer;

    [SetUp]
    public void SetUp()
    {
        var settings = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = TotalWorkUnits,
            WorkUnitsBeforeLongBreak = WorkUnitsBeforeLongBreak
        };

        _pomodoroTimer = new PomodoroTimer(Substitute.For<Action>(), settings);
    }

    [Test]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer!.Info.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(_pomodoroTimer.Info.IsRunning, Is.False);
            Assert.That(_pomodoroTimer.Info.WorkUnitsCompleted, Is.Zero);
        });
    }

    [Test]
    public void UpdateSettings_UpdatesTimersAndSettings()
    {
        // Arrange
        var newSettings = new PomodoroSettings
        {
            WorkDurationSeconds = 2,
            ShortBreakDurationSeconds = 3,
            LongBreakDurationSeconds = 4,
            TotalWorkUnits = 5,
            WorkUnitsBeforeLongBreak = 6
        };

        // Act
        _pomodoroTimer!.UpdateSettings(newSettings);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.Info.Settings.WorkDurationSeconds, Is.EqualTo(newSettings.WorkDurationSeconds));
            Assert.That(_pomodoroTimer.Info.Settings.ShortBreakDurationSeconds, Is.EqualTo(newSettings.ShortBreakDurationSeconds));
            Assert.That(_pomodoroTimer.Info.Settings.LongBreakDurationSeconds, Is.EqualTo(newSettings.LongBreakDurationSeconds));
            Assert.That(_pomodoroTimer.Info.Settings.TotalWorkUnits, Is.EqualTo(newSettings.TotalWorkUnits));
            Assert.That(_pomodoroTimer.Info.Settings.WorkUnitsBeforeLongBreak, Is.EqualTo(newSettings.WorkUnitsBeforeLongBreak));
        });
    }

    [Test]
    public void UpdateSettings_InvokesTickCallback()
    {
        // Arrange
        var wasTickCallbackInvoked = false;
        var pomodoroTimer = new PomodoroTimer(() => { wasTickCallbackInvoked = true; }, new PomodoroSettings());

        // Act
        pomodoroTimer.UpdateSettings(new PomodoroSettings());

        // Assert
        Assert.That(wasTickCallbackInvoked, Is.True);
    }

    [Test]
    public async Task PhaseCompleted_IsInvoked_WhenTimerPassesToNextPhaseOrIsSkipped()
    {
        // Arrange
        bool phaseCompletedInvoked = false;
        bool phaseWasSkipped = false;

        _pomodoroTimer!.PhaseCompleted += (_, wasSkipped) =>
        {
            phaseCompletedInvoked = true;
            phaseWasSkipped = wasSkipped;
        };

        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(WorkDurationSeconds * 1000 + TestDelayMilliseconds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(phaseCompletedInvoked, Is.True);
            Assert.That(phaseWasSkipped, Is.False);
        });

        // Reset for skip test
        phaseCompletedInvoked = false;
        phaseWasSkipped = false;

        // Act  
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimer!.SkipActiveTimer();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(phaseCompletedInvoked, Is.True);
            Assert.That(phaseWasSkipped, Is.True);
        });
    }

    [Test]
    public async Task PhaseCompleted_IsNotInvoked_WhenActiveTimerIsStoppedOrReset()
    {
        // Arrange
        bool phaseCompletedInvoked = false;
        _pomodoroTimer!.PhaseCompleted += (_, _) => phaseCompletedInvoked = true;

        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimer!.StopActiveTimer();

        // Assert
        Assert.That(phaseCompletedInvoked, Is.False);

        // Reset for reset test
        phaseCompletedInvoked = false;

        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimer!.ResetActiveTimer();

        // Assert
        Assert.That(phaseCompletedInvoked, Is.False);
    }

    [Test]
    public async Task ResetActiveTimer_ResetsActiveTimer()
    {
        // Arrange
        TimeSpan activeTimerDuration = _pomodoroTimer!.Info.RemainingTimeInCurrentPhase;

        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        _pomodoroTimer!.ResetActiveTimer();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.Info.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero));
            Assert.That(_pomodoroTimer.Info.RemainingTimeInCurrentPhase, Is.EqualTo(activeTimerDuration));
            Assert.That(_pomodoroTimer.Info.IsRunning, Is.False);
            Assert.That(_pomodoroTimer.Info.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
        });
    }

    [Test]
    public async Task ResetSession_ResetsToWorkPhase()
    {
        // Arrange
        int secondsUntilNextPhase = _pomodoroTimer!.Info.RemainingTimeInCurrentPhase.Seconds;

        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(secondsUntilNextPhase * 1000 + TestDelayMilliseconds);
        _pomodoroTimer!.ResetSession();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.Info.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(_pomodoroTimer.Info.RemainingTimeInCurrentPhase, Is.EqualTo(TimeSpan.FromSeconds(WorkDurationSeconds)));
            Assert.That(_pomodoroTimer.Info.WorkUnitsCompleted, Is.Zero);
            Assert.That(_pomodoroTimer.Info.IsRunning, Is.False);
            Assert.That(_pomodoroTimer.Info.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public async Task SessionCompleted_IsInvoked_WhenAllWorkUnitsAreCompletedOrLastWorkUnitIsSkipped()
    {
        // Arrange
        bool sessionCompletedInvoked = false;
        _pomodoroTimer!.SessionCompleted += (_, _) => sessionCompletedInvoked = true;

        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(GetTotalTimerDurationSeconds() * 1000 + TestDelayMilliseconds);

        // Assert
        Assert.That(sessionCompletedInvoked, Is.True);

        // Reset for skip test
        sessionCompletedInvoked = false;

        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay((GetTotalTimerDurationSeconds() - WorkDurationSeconds) * 1000 + TestDelayMilliseconds);
        _pomodoroTimer!.SkipActiveTimer();

        // Assert
        Assert.That(sessionCompletedInvoked, Is.True);
    }

    [Test]
    public async Task SkipActiveTimer_SkipsToNextPhase()
    {
        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimer!.SkipActiveTimer();
        var elapsedTimeAfterSkip = _pomodoroTimer.Info.ElapsedTimeInCurrentPhase;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.Info.CurrentPhase, Is.Not.EqualTo(PomodoroPhase.Work));
            Assert.That(_pomodoroTimer.Info.WorkUnitsCompleted, Is.EqualTo(1));
            Assert.That(_pomodoroTimer.Info.IsRunning, Is.True);
            Assert.That(elapsedTimeAfterSkip, Is.EqualTo(TimeSpan.Zero).Within(TimeSpan.FromMilliseconds(100)));
        });
    }

    [Test]
    public async Task StartActiveTimer_DoesNotDoAnything_WhenIsRunningIsTrue()
    {
        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimer!.StartActiveTimer();

        // Assert
        Assert.That(_pomodoroTimer.Info.ElapsedTimeInCurrentPhase, Is.Not.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void StartActiveTimer_SetsIsRunningToTrue()
    {
        _pomodoroTimer!.StartActiveTimer();
        Assert.That(_pomodoroTimer.Info.IsRunning, Is.True);
    }

    [Test]
    public void StopActiveTimer_SetsIsRunningToFalse()
    {
        // Act
        _pomodoroTimer!.StartActiveTimer();
        _pomodoroTimer.StopActiveTimer();

        // Assert
        Assert.That(_pomodoroTimer.Info.IsRunning, Is.False);
    }

    [Test]
    public async Task StopActiveTimer_StopsTheActiveTimer()
    {
        // Act
        _pomodoroTimer!.StartActiveTimer();
        _pomodoroTimer.StopActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        var elapsedTimeAfterStop = _pomodoroTimer.Info.ElapsedTimeInCurrentPhase;
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        Assert.That(_pomodoroTimer.Info.ElapsedTimeInCurrentPhase, Is.EqualTo(elapsedTimeAfterStop));
    }

    [Test]
    public void WorkPhaseIsFollowedByBreakPhase()
    {
        // Act
        _pomodoroTimer!.SkipActiveTimer();

        // Assert
        Assert.That(_pomodoroTimer.Info.CurrentPhase, Is.Not.EqualTo(PomodoroPhase.Work));
    }

    [Test]
    public async Task WorkUnitsCompleted_EqualsTotalWorkUnits_WhenSessionCompleted()
    {
        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(GetTotalTimerDurationSeconds() * 1000 + TestDelayMilliseconds);
        // Assert
        Assert.That(_pomodoroTimer.Info.WorkUnitsCompleted, Is.EqualTo(TotalWorkUnits));
    }

    [Test]
    public async Task WorkUnitsCompleted_IsUpdatedWhenTimerPassesToNextPhase()
    {
        // Arrange
        int secondsUntilNextPhase = _pomodoroTimer!.Info.RemainingTimeInCurrentPhase.Seconds;

        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(secondsUntilNextPhase * 1000 + TestDelayMilliseconds);

        // Assert
        Assert.That(_pomodoroTimer.Info.WorkUnitsCompleted, Is.EqualTo(1));
    }

    [Test]
    public async Task TickCallback_IsInvoked_WhenStartingTimer()
    {
        // Arrange
        bool wasTickCallbackInvoked = false;
        var pomodoroTimer = new PomodoroTimer(() => { wasTickCallbackInvoked = true; }, new PomodoroSettings());

        // Act
        pomodoroTimer.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        Assert.That(wasTickCallbackInvoked, Is.True);
    }

    [Test]
    public void TimerInfo_InitializesCorrectly()
    {
        // Arrange
        var info = _pomodoroTimer!.Info;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(info.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero));
            Assert.That(info.IsRunning, Is.False);
            Assert.That(info.LongBreakDuration, Is.EqualTo(LongBreakDurationSeconds));
            Assert.That(info.ProgressCurrentPhase, Is.EqualTo(0));
            Assert.That(info.RemainingTimeInCurrentPhase, Is.EqualTo(TimeSpan.FromSeconds(WorkDurationSeconds)));
            Assert.That(info.Settings.WorkDurationSeconds, Is.EqualTo(WorkDurationSeconds));
            Assert.That(info.ShortBreakDuration, Is.EqualTo(ShortBreakDurationSeconds));
            Assert.That(info.TotalElapsedTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(info.TotalRemainingTime, Is.EqualTo(info.TotalTimerDuration));
            Assert.That(info.TotalTimerDuration, Is.EqualTo(TimeSpan.FromSeconds(GetTotalTimerDurationSeconds())));
            Assert.That(info.TotalWorkUnits, Is.EqualTo(TotalWorkUnits));
            Assert.That(info.WorkUnitsBeforeLongBreak, Is.EqualTo(WorkUnitsBeforeLongBreak));
            Assert.That(info.WorkUnitsCompleted, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task TimerInfo_UpdatesCorrectly_AfterStarting()
    {
        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        var info = _pomodoroTimer.Info;
        Assert.Multiple(() =>
        {
            Assert.That(info.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(info.ElapsedTimeInCurrentPhase, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(info.IsRunning, Is.True);
            Assert.That(info.ProgressCurrentPhase, Is.GreaterThan(0));
            Assert.That(info.RemainingTimeInCurrentPhase, Is.LessThan(TimeSpan.FromSeconds(WorkDurationSeconds)));
            Assert.That(info.TotalElapsedTime, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(info.TotalRemainingTime, Is.LessThan(info.TotalTimerDuration));
            Assert.That(info.WorkUnitsCompleted, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task TimerInfo_ResetsCorrectly()
    {
        // Act
        _pomodoroTimer!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimer.ResetActiveTimer();

        // Assert
        var info = _pomodoroTimer.Info;
        Assert.Multiple(() =>
        {
            Assert.That(info.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(info.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero));
            Assert.That(info.IsRunning, Is.False);
            Assert.That(info.ProgressCurrentPhase, Is.EqualTo(0));
            Assert.That(info.RemainingTimeInCurrentPhase, Is.EqualTo(TimeSpan.FromSeconds(WorkDurationSeconds)));
            Assert.That(info.TotalElapsedTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(info.TotalRemainingTime, Is.EqualTo(info.TotalTimerDuration));
            Assert.That(info.WorkUnitsCompleted, Is.EqualTo(0));
        });
    }
}
