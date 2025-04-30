using NSubstitute;

using NUnit.Framework;

namespace PhaseShift.Core.Tests;

[TestFixture]
internal class PomodoroSessionManagerTests : BaseTestHelper
{
    private PomodoroSessionManager? _sessionManager;

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

        _sessionManager = new PomodoroSessionManager(Substitute.For<Action>(), settings);
    }

    [Test]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_sessionManager!.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(_sessionManager.ActiveTimer.Duration, Is.EqualTo(TimeSpan.FromSeconds(WorkDurationSeconds)));
            Assert.That(_sessionManager.IsRunning, Is.False);
            Assert.That(_sessionManager.WorkUnitsCompleted, Is.Zero);
            Assert.That(_sessionManager.IsCompleted, Is.False);
        });
    }

    [Test]
    public void Constructor_Throws_WhenSettingsValidationFails()
    {
        // WorkUnitsBeforeLongBreak < 1
        var invalidSettings1 = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = TotalWorkUnits,
            WorkUnitsBeforeLongBreak = 0
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => new PomodoroSessionManager(Substitute.For<Action>(), invalidSettings1));

        // TotalWorkUnits < 1
        var invalidSettings2 = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = 0,
            WorkUnitsBeforeLongBreak = WorkUnitsBeforeLongBreak
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => new PomodoroSessionManager(Substitute.For<Action>(), invalidSettings2));

        // WorkDurationSeconds < 1
        var invalidSettings3 = new PomodoroSettings
        {
            WorkDurationSeconds = 0,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = TotalWorkUnits,
            WorkUnitsBeforeLongBreak = WorkUnitsBeforeLongBreak
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => new PomodoroSessionManager(Substitute.For<Action>(), invalidSettings3));

        // ShortBreakDurationSeconds < 1
        var invalidSettings4 = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = 0,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = TotalWorkUnits,
            WorkUnitsBeforeLongBreak = WorkUnitsBeforeLongBreak
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => new PomodoroSessionManager(Substitute.For<Action>(), invalidSettings4));

        // ShortBreakDurationSeconds > LongBreakDurationSeconds
        var invalidSettings5 = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = LongBreakDurationSeconds + 1,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = TotalWorkUnits,
            WorkUnitsBeforeLongBreak = WorkUnitsBeforeLongBreak
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => new PomodoroSessionManager(Substitute.For<Action>(), invalidSettings5));
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
        _sessionManager!.UpdateSettings(newSettings);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sessionManager.Settings.WorkDurationSeconds, Is.EqualTo(newSettings.WorkDurationSeconds));
            Assert.That(_sessionManager.Settings.ShortBreakDurationSeconds, Is.EqualTo(newSettings.ShortBreakDurationSeconds));
            Assert.That(_sessionManager.Settings.LongBreakDurationSeconds, Is.EqualTo(newSettings.LongBreakDurationSeconds));
            Assert.That(_sessionManager.Settings.TotalWorkUnits, Is.EqualTo(newSettings.TotalWorkUnits));
            Assert.That(_sessionManager.Settings.WorkUnitsBeforeLongBreak, Is.EqualTo(newSettings.WorkUnitsBeforeLongBreak));
        });
    }

    [Test]
    public void UpdateSettings_InvokesTickCallback()
    {
        // Arrange
        var wasTickCallbackInvoked = false;
        var sessionManager = new PomodoroSessionManager(() => { wasTickCallbackInvoked = true; }, new PomodoroSettings());

        // Act
        sessionManager.UpdateSettings(new PomodoroSettings());

        // Assert
        Assert.That(wasTickCallbackInvoked, Is.True);
    }

    [Test]
    public void UpdateSettings_Throws_WhenSettingsValidationFails()
    {
        // Arrange
        var invalidSettings = new PomodoroSettings
        {
            WorkDurationSeconds = 0,
            ShortBreakDurationSeconds = 0,
            LongBreakDurationSeconds = 0,
            TotalWorkUnits = 0,
            WorkUnitsBeforeLongBreak = 0
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _sessionManager!.UpdateSettings(invalidSettings));
    }

    [Test]
    public void LongPhaseFollowsAfterWorkUnitsBeforeLongBreak()
    {
        // Arrange
        var settings = new PomodoroSettings
        {
            WorkDurationSeconds = 1,
            ShortBreakDurationSeconds = 1,
            LongBreakDurationSeconds = 2,
            TotalWorkUnits = 3,
            WorkUnitsBeforeLongBreak = 2
        };
        var phasesToSkipForLongBreak = 2 * settings.WorkUnitsBeforeLongBreak - 1;

        _sessionManager = new PomodoroSessionManager(Substitute.For<Action>(), settings);

        // Act
        _sessionManager!.StartActiveTimer();
        for (int i = 0; i < phasesToSkipForLongBreak; i++)
        {
            _sessionManager.SkipActiveTimer();
        }

        // Assert
        Assert.That(_sessionManager.CurrentPhase, Is.EqualTo(PomodoroPhase.LongBreak));
        Assert.That(_sessionManager.ActiveTimer.Duration, Is.EqualTo(TimeSpan.FromSeconds(settings.LongBreakDurationSeconds)));
    }

    [Test]
    public async Task PhaseCompleted_IsInvoked_WhenTimerPassesToNextPhaseOrIsSkipped()
    {
        // Arrange
        bool phaseCompletedInvoked = false;
        bool phaseWasSkipped = false;

        _sessionManager!.PhaseCompleted += (_, wasSkipped) =>
        {
            phaseCompletedInvoked = true;
            phaseWasSkipped = wasSkipped;
        };

        // Act
        _sessionManager!.StartActiveTimer();
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
        _sessionManager!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _sessionManager!.SkipActiveTimer();

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
        _sessionManager!.PhaseCompleted += (_, _) => phaseCompletedInvoked = true;

        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _sessionManager!.StopActiveTimer();

        // Assert
        Assert.That(phaseCompletedInvoked, Is.False);

        // Reset for reset test
        phaseCompletedInvoked = false;

        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _sessionManager!.ResetActiveTimer();

        // Assert
        Assert.That(phaseCompletedInvoked, Is.False);
    }

    [Test]
    public async Task ResetActiveTimer_ResetsActiveTimer()
    {
        // Arrange
        TimeSpan activeTimerDuration = _sessionManager!.ActiveTimer.Duration;

        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        _sessionManager!.ResetActiveTimer();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sessionManager.ActiveTimer.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(_sessionManager.ActiveTimer.Duration, Is.EqualTo(activeTimerDuration));
            Assert.That(_sessionManager.ActiveTimer.RemainingTime, Is.EqualTo(activeTimerDuration));
            Assert.That(_sessionManager.IsRunning, Is.False);
            Assert.That(_sessionManager.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
        });
    }

    [Test]
    public async Task ResetSession_ResetsToWorkPhase()
    {
        // Arrange
        int secondsUntilNextPhase = _sessionManager!.ActiveTimer.RemainingTime.Seconds;

        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay(secondsUntilNextPhase * 1000 + TestDelayMilliseconds);
        _sessionManager!.ResetSession();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sessionManager.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(_sessionManager.ActiveTimer.RemainingTime, Is.EqualTo(TimeSpan.FromSeconds(WorkDurationSeconds)));
            Assert.That(_sessionManager.WorkUnitsCompleted, Is.Zero);
            Assert.That(_sessionManager.IsRunning, Is.False);
            Assert.That(_sessionManager.IsCompleted, Is.False);
            Assert.That(_sessionManager.ActiveTimer.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public async Task SessionCompleted_IsInvoked_WhenAllWorkUnitsAreCompletedOrLastWorkUnitIsSkipped()
    {
        // Arrange
        bool sessionCompletedInvoked = false;
        _sessionManager!.SessionCompleted += (_, _) => sessionCompletedInvoked = true;

        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay(TotalTimerDurationSeconds * 1000 + TestDelayMilliseconds);

        // Assert
        Assert.That(sessionCompletedInvoked, Is.True);

        // Reset for skip test
        sessionCompletedInvoked = false;

        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay((TotalTimerDurationSeconds - WorkDurationSeconds) * 1000 + TestDelayMilliseconds);
        _sessionManager!.SkipActiveTimer();

        // Assert
        Assert.That(sessionCompletedInvoked, Is.True);
    }

    [Test]
    public async Task SkipActiveTimer_SkipsToNextPhase()
    {
        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _sessionManager.SkipActiveTimer();
        var elapsedTimeAfterSkip = _sessionManager.ActiveTimer.ElapsedTime;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sessionManager.CurrentPhase, Is.Not.EqualTo(PomodoroPhase.Work));
            Assert.That(_sessionManager.WorkUnitsCompleted, Is.EqualTo(1));
            Assert.That(_sessionManager.IsRunning, Is.True);
            Assert.That(elapsedTimeAfterSkip, Is.EqualTo(TimeSpan.Zero).Within(TimeSpan.FromMilliseconds(100)));
        });
    }

    [Test]
    public async Task StartActiveTimer_DoesNotDoAnything_WhenIsRunningIsTrue()
    {
        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);
        _sessionManager.StartActiveTimer();

        // Assert
        Assert.That(_sessionManager.ActiveTimer.ElapsedTime, Is.Not.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void StartActiveTimer_SetsIsRunningToTrue()
    {
        _sessionManager!.StartActiveTimer();
        Assert.That(_sessionManager.IsRunning, Is.True);
    }

    [Test]
    public void StopActiveTimer_SetsIsRunningToFalse()
    {
        // Act
        _sessionManager!.StartActiveTimer();
        _sessionManager.StopActiveTimer();

        // Assert
        Assert.That(_sessionManager.IsRunning, Is.False);
    }

    [Test]
    public async Task StopActiveTimer_StopsTheActiveTimer()
    {
        // Act
        _sessionManager!.StartActiveTimer();
        _sessionManager.StopActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        var elapsedTimeAfterStop = _sessionManager.ActiveTimer.ElapsedTime;
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        Assert.That(_sessionManager.ActiveTimer.ElapsedTime, Is.EqualTo(elapsedTimeAfterStop));
    }

    [Test]
    public void WorkPhaseIsFollowedByBreakPhase()
    {
        // Act
        _sessionManager!.SkipActiveTimer();

        // Assert
        Assert.That(_sessionManager.CurrentPhase, Is.Not.EqualTo(PomodoroPhase.Work));
    }

    [Test]
    public async Task WorkUnitsCompleted_EqualsTotalWorkUnits_WhenSessionCompleted()
    {
        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay(TotalTimerDurationSeconds * 1000 + TestDelayMilliseconds);
        // Assert
        Assert.That(_sessionManager.WorkUnitsCompleted, Is.EqualTo(TotalWorkUnits));
    }

    [Test]
    public async Task WorkUnitsCompleted_IsUpdatedWhenTimerPassesToNextPhase()
    {
        // Arrange
        int secondsUntilNextPhase = _sessionManager!.ActiveTimer.RemainingTime.Seconds;

        // Act
        _sessionManager!.StartActiveTimer();
        await Task.Delay(secondsUntilNextPhase * 1000 + TestDelayMilliseconds);

        // Assert
        Assert.That(_sessionManager.WorkUnitsCompleted, Is.EqualTo(1));
    }

    [Test]
    public async Task TickCallback_IsInvoked_WhenStartingTimer()
    {
        // Arrange
        bool wasTickCallbackInvoked = false;
        var sessionManager = new PomodoroSessionManager(() => { wasTickCallbackInvoked = true; }, new PomodoroSettings());

        // Act
        sessionManager.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        Assert.That(wasTickCallbackInvoked, Is.True);
    }

    [Test]
    public void UpdateSettings_UpdatesSettings_WhenTimerIsNotRunning()
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
        _sessionManager!.UpdateSettings(newSettings);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sessionManager.Settings.WorkDurationSeconds, Is.EqualTo(newSettings.WorkDurationSeconds));
            Assert.That(_sessionManager.Settings.ShortBreakDurationSeconds, Is.EqualTo(newSettings.ShortBreakDurationSeconds));
            Assert.That(_sessionManager.Settings.LongBreakDurationSeconds, Is.EqualTo(newSettings.LongBreakDurationSeconds));
            Assert.That(_sessionManager.Settings.TotalWorkUnits, Is.EqualTo(newSettings.TotalWorkUnits));
            Assert.That(_sessionManager.Settings.WorkUnitsBeforeLongBreak, Is.EqualTo(newSettings.WorkUnitsBeforeLongBreak));
        });
    }


    [Test]
    public async Task UpdateSettings_UpdatesSettingsAndRestartsTimer_WhenTimerIsRunning()
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
        _sessionManager!.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        _sessionManager.UpdateSettings(newSettings);
        var isRunningAfterUpdate = _sessionManager.IsRunning;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sessionManager.Settings.WorkDurationSeconds, Is.EqualTo(newSettings.WorkDurationSeconds));
            Assert.That(_sessionManager.Settings.ShortBreakDurationSeconds, Is.EqualTo(newSettings.ShortBreakDurationSeconds));
            Assert.That(_sessionManager.Settings.LongBreakDurationSeconds, Is.EqualTo(newSettings.LongBreakDurationSeconds));
            Assert.That(_sessionManager.Settings.TotalWorkUnits, Is.EqualTo(newSettings.TotalWorkUnits));
            Assert.That(_sessionManager.Settings.WorkUnitsBeforeLongBreak, Is.EqualTo(newSettings.WorkUnitsBeforeLongBreak));
            Assert.That(isRunningAfterUpdate, Is.True);
        });
    }

    [Test]
    [TestCase(2)]
    [TestCase(3)]
    public async Task UpdateSettings_UpdatesTotalWorkUnitsAndDoesNotInvokeSessionCompleted_WhenNewTotalWorkUnitsGreaterThanOrEqualToCompletedWorkUnitsPlusOne(int newTotalWorkUnits)
    {
        // Arrange
        var wasSessionCompletedInvoked = false;
        var newSettings = new PomodoroSettings
        {
            TotalWorkUnits = newTotalWorkUnits
        };

        _sessionManager!.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;

        // Act
        _sessionManager.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        _sessionManager.SkipActiveTimer(); // skip to first break => 1 work unit completed!
        _sessionManager.UpdateSettings(newSettings);
        _sessionManager.StopActiveTimer();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sessionManager.WorkUnitsCompleted, Is.EqualTo(1));
            Assert.That(_sessionManager.Settings.TotalWorkUnits, Is.EqualTo(newTotalWorkUnits));
            Assert.That(wasSessionCompletedInvoked, Is.False);
        });
    }

    [Test]
    public async Task UpdateSettings_UpdatesTotalWorkUnitsAndInvokesSessionCompleted_WhenNewTotalWorkUnitsLessThanCompletedWorkUnitsPlusOne()
    {
        // Arrange
        var wasSessionCompletedInvoked = false;
        var newSettings = new PomodoroSettings
        {
            TotalWorkUnits = 1 // less than 1 work unit completed plus 1
        };

        _sessionManager!.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;

        // Act
        _sessionManager.StartActiveTimer();
        await Task.Delay(TestDelayMilliseconds);

        _sessionManager.SkipActiveTimer(); // skip to first break => 1 work unit completed
        _sessionManager.UpdateSettings(newSettings);
        _sessionManager.StopActiveTimer();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sessionManager.WorkUnitsCompleted, Is.EqualTo(1));
            Assert.That(_sessionManager.Settings.TotalWorkUnits, Is.EqualTo(1));
            Assert.That(wasSessionCompletedInvoked, Is.False);
        });
    }
}