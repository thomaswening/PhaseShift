using NUnit.Framework;

using PhaseShift.Core;
using PhaseShift.UI.PomodoroFeature;
using PhaseShift.UI.Tests.Mocks;

namespace PhaseShift.UI.Tests.PomodoroFeature;

[TestFixture]
internal class PomodoroTimerVmTests
{
    protected const int LongBreakDurationSeconds = 1;
    protected const int TestDelayMilliseconds = 200;
    protected const int ShortBreakDurationSeconds = 1;
    protected const int TotalWorkUnits = 2;
    protected const int WorkDurationSeconds = 1;
    protected const int WorkUnitsBeforeLongBreak = 1;

    private PomodoroTimerVm? _pomodoroTimerVm;

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

        _pomodoroTimerVm = new PomodoroTimerVm(settings, new MockDispatcher());
    }

    [Test]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimerVm!.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(_pomodoroTimerVm.IsRunning, Is.False);
            Assert.That(_pomodoroTimerVm.WorkUnitsCompleted, Is.Zero);
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
        _pomodoroTimerVm!.UpdateSettings(newSettings);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimerVm.TotalWorkUnits, Is.EqualTo(newSettings.TotalWorkUnits));
            Assert.That(_pomodoroTimerVm.WorkUnitsBeforeLongBreak, Is.EqualTo(newSettings.WorkUnitsBeforeLongBreak));
            Assert.That(_pomodoroTimerVm.ShortBreakEqualsLongBreak, Is.EqualTo(newSettings.LongBreakDurationSeconds == newSettings.ShortBreakDurationSeconds));
        });
    }

    [Test]
    public async Task PhaseCompleted_IsInvoked_WhenTimerPassesToNextPhase()
    {
        // Arrange
        bool phaseCompletedInvoked = false;
        bool phaseWasSkipped = false;

        _pomodoroTimerVm!.PomodoroPhaseCompleted += (_, args) =>
        {
            phaseCompletedInvoked = true;
        };

        // Act
        _pomodoroTimerVm.StartTimerCommand.Execute(null);
        await Task.Delay(WorkDurationSeconds * 1000 + TestDelayMilliseconds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(phaseCompletedInvoked, Is.True);
            Assert.That(phaseWasSkipped, Is.False);
        });
    }

    [Test]
    public async Task PhaseCompleted_IsNotInvoked_WhenPhaseIsSkipped()
    {
        // Arrange
        var phaseCompletedInvoked = false;

        _pomodoroTimerVm!.PomodoroPhaseCompleted += (_, args) =>
        {
            phaseCompletedInvoked = true;
        };

        // Act
        _pomodoroTimerVm.StartTimerCommand.Execute(null);
        await Task.Delay(TestDelayMilliseconds);

        _pomodoroTimerVm.SkipToNextPhaseCommand.Execute(null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(phaseCompletedInvoked, Is.False);
        });
    }

    [Test]
    public async Task PhaseCompleted_IsNotInvoked_WhenActiveTimerIsStoppedOrReset()
    {
        // Arrange
        bool phaseCompletedInvoked = false;
        _pomodoroTimerVm!.PomodoroPhaseCompleted += (_, _) => phaseCompletedInvoked = true;

        // Act
        _pomodoroTimerVm.StartTimerCommand.Execute(null);
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimerVm.StopTimerCommand.Execute(null);

        // Assert
        Assert.That(phaseCompletedInvoked, Is.False);

        // Reset for reset test
        phaseCompletedInvoked = false;

        // Act
        _pomodoroTimerVm.StartTimerCommand.Execute(null);
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimerVm.ResetCurrentPhaseCommand.Execute(null);

        // Assert
        Assert.That(phaseCompletedInvoked, Is.False);
    }

    [Test]
    public async Task ResetCurrentPhase_ResetsActiveTimer()
    {
        // Arrange
        TimeSpan activeTimerDuration = _pomodoroTimerVm!.RemainingTimeInCurrentPhase;

        // Act
        _pomodoroTimerVm.StartTimerCommand.Execute(null);
        await Task.Delay(TestDelayMilliseconds);

        _pomodoroTimerVm.ResetCurrentPhaseCommand.Execute(null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimerVm.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero));
            Assert.That(_pomodoroTimerVm.RemainingTimeInCurrentPhase, Is.EqualTo(activeTimerDuration));
            Assert.That(_pomodoroTimerVm.IsRunning, Is.False);
            Assert.That(_pomodoroTimerVm.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
        });
    }

    [Test]
    public async Task ResetSession_ResetsToWorkPhase()
    {
        // Arrange
        int secondsUntilNextPhase = _pomodoroTimerVm!.RemainingTimeInCurrentPhase.Seconds;

        // Act
        _pomodoroTimerVm.StartTimerCommand.Execute(null);
        await Task.Delay(secondsUntilNextPhase * 1000 + TestDelayMilliseconds);
        _pomodoroTimerVm.ResetSessionCommand.Execute(null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimerVm.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(_pomodoroTimerVm.RemainingTimeInCurrentPhase, Is.EqualTo(TimeSpan.FromSeconds(WorkDurationSeconds)));
            Assert.That(_pomodoroTimerVm.WorkUnitsCompleted, Is.Zero);
            Assert.That(_pomodoroTimerVm.IsRunning, Is.False);
            Assert.That(_pomodoroTimerVm.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public async Task SessionCompleted_IsInvoked_WhenAllWorkUnitsAreCompleted()
    {
        // Arrange
        bool sessionCompletedInvoked = false;
        _pomodoroTimerVm!.PomodoroPhaseCompleted += (_, _) => sessionCompletedInvoked = true;

        // Act
        _pomodoroTimerVm.StartTimerCommand.Execute(null);
        await Task.Delay((int)_pomodoroTimerVm.SessionDuration.TotalMilliseconds + TestDelayMilliseconds);

        // Assert
        Assert.That(sessionCompletedInvoked, Is.True);
    }

    [Test]
    public async Task SessionCompleted_IsInvoked_WhenLastWorkUnitIsSkipped()
    {
        // Arrange
        bool sessionCompletedInvoked = false;
        _pomodoroTimerVm!.PomodoroPhaseCompleted += (_, _) => sessionCompletedInvoked = true;

        // Act
        _pomodoroTimerVm.StartTimerCommand.Execute(null);
        await Task.Delay((int)_pomodoroTimerVm.SessionDuration.TotalMilliseconds + TestDelayMilliseconds - WorkDurationSeconds * 1000 + TestDelayMilliseconds);
        _pomodoroTimerVm.SkipToNextPhaseCommand.Execute(null);

        // Assert
        Assert.That(sessionCompletedInvoked, Is.True);
    }

    [Test]
    public async Task SkipToNextPhase_SkipsToNextPhase()
    {
        // Act
        _pomodoroTimerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimerVm.SkipToNextPhaseCommand.Execute(null);
        var elapsedTimeAfterSkip = _pomodoroTimerVm.ElapsedTimeInCurrentPhase;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimerVm.CurrentPhase, Is.Not.EqualTo(PomodoroPhase.Work));
            Assert.That(_pomodoroTimerVm.WorkUnitsCompleted, Is.EqualTo(1));
            Assert.That(_pomodoroTimerVm.IsRunning, Is.True);
            Assert.That(elapsedTimeAfterSkip, Is.EqualTo(TimeSpan.Zero).Within(TimeSpan.FromMilliseconds(100)));
        });
    }

    [Test]
    public async Task StartTimer_DoesNotDoAnything_WhenIsRunningIsTrue()
    {
        // Act
        _pomodoroTimerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimerVm.StartTimerCommand.Execute(null);

        // Assert
        Assert.That(_pomodoroTimerVm.ElapsedTimeInCurrentPhase, Is.Not.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void StartTimer_SetsIsRunningToTrue()
    {
        _pomodoroTimerVm!.StartTimerCommand.Execute(null);
        Assert.That(_pomodoroTimerVm.IsRunning, Is.True);
    }

    [Test]
    public void StopTimer_SetsIsRunningToFalse()
    {
        // Act
        _pomodoroTimerVm!.StartTimerCommand.Execute(null);
        _pomodoroTimerVm.StopTimerCommand.Execute(null);

        // Assert
        Assert.That(_pomodoroTimerVm.IsRunning, Is.False);
    }

    [Test]
    public async Task StopTimer_StopsTheActiveTimer()
    {
        // Act
        _pomodoroTimerVm!.StartTimerCommand.Execute(null);
        _pomodoroTimerVm.StopTimerCommand.Execute(null);
        await Task.Delay(TestDelayMilliseconds);

        var elapsedTimeAfterStop = _pomodoroTimerVm.ElapsedTimeInCurrentPhase;
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        Assert.That(_pomodoroTimerVm.ElapsedTimeInCurrentPhase, Is.EqualTo(elapsedTimeAfterStop));
    }

    [Test]
    public void WorkPhaseIsFollowedByBreakPhase()
    {
        // Act
        _pomodoroTimerVm!.SkipToNextPhaseCommand.Execute(null);

        // Assert
        Assert.That(_pomodoroTimerVm.CurrentPhase, Is.Not.EqualTo(PomodoroPhase.Work));
    }

    [Test]
    public async Task WorkUnitsCompleted_EqualsTotalWorkUnits_WhenSessionCompleted()
    {
        // Act
        _pomodoroTimerVm!.StartTimerCommand.Execute(null);
        await Task.Delay((int)_pomodoroTimerVm.SessionDuration.TotalMilliseconds + TestDelayMilliseconds);

        // Assert
        Assert.That(_pomodoroTimerVm.WorkUnitsCompleted, Is.EqualTo(TotalWorkUnits));
    }

    [Test]
    public async Task WorkUnitsCompleted_IsUpdatedWhenTimerPassesToNextPhase()
    {
        // Arrange
        int secondsUntilNextPhase = _pomodoroTimerVm!.RemainingTimeInCurrentPhase.Seconds;

        // Act
        _pomodoroTimerVm.StartTimerCommand.Execute(null);
        await Task.Delay(secondsUntilNextPhase * 1000 + TestDelayMilliseconds);

        // Assert
        Assert.That(_pomodoroTimerVm.WorkUnitsCompleted, Is.EqualTo(1));
    }

    [Test]
    public void TimerInfo_InitializesCorrectly()
    {
        // Arrange
        var info = _pomodoroTimerVm!;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(info.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero));
            Assert.That(info.IsRunning, Is.False);
            Assert.That(info.TotalWorkUnits, Is.EqualTo(TotalWorkUnits));
            Assert.That(info.WorkUnitsBeforeLongBreak, Is.EqualTo(WorkUnitsBeforeLongBreak));
            Assert.That(info.WorkUnitsCompleted, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task TimerInfo_UpdatesCorrectly_AfterStarting()
    {
        // Act
        _pomodoroTimerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        var info = _pomodoroTimerVm;
        Assert.Multiple(() =>
        {
            Assert.That(info.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(info.ElapsedTimeInCurrentPhase, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(info.IsRunning, Is.True);
            Assert.That(info.RemainingTimeInCurrentPhase, Is.LessThan(TimeSpan.FromSeconds(WorkDurationSeconds)));
            Assert.That(info.ElapsedTimeInSession, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(info.RemainingTimeInSession, Is.LessThan(info.SessionDuration));
            Assert.That(info.WorkUnitsCompleted, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task TimerInfo_ResetsCorrectly()
    {
        // Act
        _pomodoroTimerVm!.StartTimerCommand.Execute(null);
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimerVm.ResetCurrentPhaseCommand.Execute(null);

        // Assert
        var info = _pomodoroTimerVm;
        Assert.Multiple(() =>
        {
            Assert.That(info.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(info.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero));
            Assert.That(info.IsRunning, Is.False);
            Assert.That(info.RemainingTimeInCurrentPhase, Is.EqualTo(TimeSpan.FromSeconds(WorkDurationSeconds)));
            Assert.That(info.ElapsedTimeInSession, Is.EqualTo(TimeSpan.Zero));
            Assert.That(info.RemainingTimeInSession, Is.EqualTo(info.SessionDuration));
            Assert.That(info.WorkUnitsCompleted, Is.EqualTo(0));
        });
    }
}
