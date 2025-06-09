using NSubstitute;

using NUnit.Framework;

namespace PhaseShift.Core.Tests;

[TestFixture]
internal class PomodoroTimerTests : BaseTestHelper
{
    private static readonly PomodoroSettings _settings = new()
    {
        WorkDurationSeconds = WorkDurationSeconds,
        ShortBreakDurationSeconds = ShortBreakDurationSeconds,
        LongBreakDurationSeconds = LongBreakDurationSeconds,
        TotalWorkUnits = TotalWorkUnits,
        WorkUnitsBeforeLongBreak = WorkUnitsBeforeLongBreak
    };

    private PomodoroTimer? _pomodoroTimer;

    [SetUp]
    public void SetUp()
    {
        _pomodoroTimer = new PomodoroTimer(Substitute.For<Action<TimeSpan>>(), _settings);
    }

    [Test]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer!.CurrentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(_pomodoroTimer.IsRunning, Is.False);
            Assert.That(_pomodoroTimer.CompletedWorkUnits, Is.Zero);
            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero));
            Assert.That(_pomodoroTimer.ElapsedTimeInSession, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    [TestCase(0, 1, 1, 1, 1)]
    [TestCase(1, 0, 1, 1, 1)]
    [TestCase(1, 1, 0, 1, 1)]
    [TestCase(1, 1, 1, 0, 1)]
    [TestCase(1, 1, 1, 1, 0)]
    public void Constructor_Throws_WhenInvalidSettingsProvided(int workSeconds, int shortBreakSeconds, int longBreakSeconds, int totalWorkUnits, int workUnitsBeforeLongBreak)
    {
        var invalidSettings = new PomodoroSettings
        {
            WorkDurationSeconds = workSeconds,
            ShortBreakDurationSeconds = shortBreakSeconds,
            LongBreakDurationSeconds = longBreakSeconds,
            TotalWorkUnits = totalWorkUnits,
            WorkUnitsBeforeLongBreak = workUnitsBeforeLongBreak
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new PomodoroTimer(Substitute.For<Action<TimeSpan>>(), invalidSettings));
    }

    [Test]
    public async Task Start_ShouldStartTimer()
    {
        // Arrange
        var wasPhaseCompletedInvoked = false;
        var wasSessionCompletedInvoked = false;
        var wasPhaseSkippedInvoked = false;
        var isRunning = false;

        _pomodoroTimer!.PhaseCompleted += (_, _) => wasPhaseCompletedInvoked = true;
        _pomodoroTimer.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;
        _pomodoroTimer.PhaseSkipped += (_, _) => wasPhaseSkippedInvoked = true;

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds);
        isRunning = _pomodoroTimer.IsRunning;

        _pomodoroTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isRunning, Is.True,
                "Timer should be running after Start is called.");

            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time in current phase should be greater than zero after starting.");

            Assert.That(_pomodoroTimer.RemainingTimeInCurrentPhase.TotalSeconds, Is.LessThan(_pomodoroTimer.Settings.WorkDurationSeconds),
                "Remaining time in current phase should be less than work duration after starting.");

            Assert.That(_pomodoroTimer.ElapsedTimeInSession, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time in session should be greater than zero after starting.");

            Assert.That(_pomodoroTimer.RemainingTimeInSession, Is.EqualTo(_pomodoroTimer.SessionDuration - _pomodoroTimer.ElapsedTimeInSession),
                "Remaining time in session should equal session duration minus elapsed time in session after starting.");

            Assert.That(_pomodoroTimer.CompletedWorkUnits, Is.EqualTo(0),
                "Completed work units should be zero after starting the timer.");

            Assert.That(wasPhaseSkippedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseSkipped)} event should not be invoked when starting the timer.");

            Assert.That(wasPhaseCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked when starting the timer.");

            Assert.That(wasSessionCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should not be invoked when starting the timer.");
        });
    }

    [Test]
    public async Task Start_ShouldDoNothing_WhenAlreadyRunning()
    {
        // Arrange
        var wasPhaseCompletedInvoked = false;
        var wasSessionCompletedInvoked = false;
        var wasPhaseSkippedInvoked = false;
        var isStillRunning = false;

        _pomodoroTimer!.PhaseCompleted += (_, _) => wasPhaseCompletedInvoked = true;
        _pomodoroTimer.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;
        _pomodoroTimer.PhaseSkipped += (_, _) => wasPhaseSkippedInvoked = true;

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimer.Start(); // Attempt to start again
        isStillRunning = _pomodoroTimer.IsRunning;

        _pomodoroTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isStillRunning, Is.True,
                "Timer should still be running.");

            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time should not be reset.");

            Assert.That(_pomodoroTimer.ElapsedTimeInSession, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time in session should not be reset.");

            Assert.That(_pomodoroTimer.RemainingTimeInSession, Is.EqualTo(_pomodoroTimer.SessionDuration - _pomodoroTimer.ElapsedTimeInSession),
                "Remaining time in session should equal session duration minus elapsed time in session after starting.");

            Assert.That(wasPhaseSkippedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseSkipped)} event should not be invoked when starting an already running timer.");

            Assert.That(wasPhaseCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked when starting an already running timer.");

            Assert.That(wasSessionCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should not be invoked when starting an already running timer.");
        });
    }

    [Test]
    public async Task Start_ShouldTriggerTickCallbackWithElapsedTime()
    {
        // Arrange
        bool wasTriggered = false;
        var elapsedTime = TimeSpan.Zero;

        void tickCallback(TimeSpan time)
        {
            wasTriggered = true;
            elapsedTime = time;
        }

        var settings = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = 1,
            WorkUnitsBeforeLongBreak = 1
        };

        var pomodoroTimer = new PomodoroTimer(tickCallback, settings);

        // Act
        pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds);
        pomodoroTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(wasTriggered, Is.True,
                "Tick callback was not triggered.");

            Assert.That(elapsedTime, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time should be greater than zero.");

            Assert.That(elapsedTime.TotalMilliseconds, Is.EqualTo(TestDelayMilliseconds).Within(10).Percent,
                "Elapsed time should match the delay used in the test.");
        });
    }

    [Test]
    public async Task Start_ShouldAdvanceToNextPhase_WhenCurrentPhaseCompletes()
    {
        // Arrange
        var wasPhaseCompletedInvoked = false;
        var wasSessionCompletedInvoked = false;
        var wasPhaseSkippedInvoked = false;
        var isRunning = false;

        _pomodoroTimer!.PhaseCompleted += (_, _) => wasPhaseCompletedInvoked = true;
        _pomodoroTimer.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;
        _pomodoroTimer.PhaseSkipped += (_, _) => wasPhaseSkippedInvoked = true;

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(1000 * WorkDurationSeconds + TestDelayMilliseconds);
        isRunning = _pomodoroTimer.IsRunning;
        _pomodoroTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.CurrentPhase, Is.Not.EqualTo(PomodoroPhase.Work),
                "Current phase should be a break after first work phase.");

            Assert.That(isRunning, Is.True,
                "Timer should still be running after advancing to the next phase.");

            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase.TotalMilliseconds, Is.LessThan(TestDelayMilliseconds).Within(10).Percent,
                "Elapsed time in current phase should be reset after advancing to the next phase.");

            Assert.That(_pomodoroTimer.ElapsedTimeInSession.TotalSeconds, Is.GreaterThan(WorkDurationSeconds),
                "Elapsed time in session should be greater than work duration after first work phase.");

            Assert.That(_pomodoroTimer.RemainingTimeInSession, Is.EqualTo(_pomodoroTimer.SessionDuration - _pomodoroTimer.ElapsedTimeInSession),
                "Remaining time in session should equal session duration minus elapsed time in session after first work phase.");

            Assert.That(_pomodoroTimer.CompletedWorkUnits, Is.EqualTo(1),
                "Completed work units should be 1 after first work phase.");

            Assert.That(wasPhaseCompletedInvoked, Is.True,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event was not invoked after the phase completed.");

            Assert.That(wasSessionCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked unless the session is fully completed.");

            Assert.That(wasPhaseSkippedInvoked, Is.False,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should not be invoked when the phase completes normally.");
        });
    }

    [Test]
    public async Task Start_ShouldNotInvokePhaseCompleted_WhenPhaseNotCompleted()
    {
        // Arrange
        var wasInvoked = false;
        _pomodoroTimer!.PhaseCompleted += (_, _) => wasInvoked = true;

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        Assert.That(wasInvoked, Is.False, $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked before the phase is completed.");
    }

    [Test]
    public async Task Start_ShouldStartLongBreak_WhenWorkUnitsBeforeLongBreakReached()
    {
        // Arrange
        var longBreakDurationSeconds = 5 * ShortBreakDurationSeconds;

        var settings = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = longBreakDurationSeconds,
            TotalWorkUnits = 3,
            WorkUnitsBeforeLongBreak = 2
        };

        var secondsToWaitForLongBreak = 2 * WorkDurationSeconds + ShortBreakDurationSeconds;
        var pomodoroTimer = new PomodoroTimer(Substitute.For<Action<TimeSpan>>(), settings);

        // Act
        pomodoroTimer.Start();
        await Task.Delay(1000 * secondsToWaitForLongBreak + TestDelayMilliseconds); // wait for long break to start
        pomodoroTimer.Stop();

        var currentPhaseDuration = pomodoroTimer.RemainingTimeInCurrentPhase + pomodoroTimer.ElapsedTimeInCurrentPhase;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(pomodoroTimer.CurrentPhase, Is.EqualTo(PomodoroPhase.LongBreak),
                "Current phase should be LongBreak after completing the first work unit.");

            Assert.That(currentPhaseDuration.TotalSeconds, Is.EqualTo(longBreakDurationSeconds),
                "Current phase duration should equal long break duration after completing the required work units.");
        });
    }

    [Test]
    public async Task Start_ShouldCompleteSession_WhenSessionCompleted()
    {
        // Arrange
        var wasInvoked = false;
        _pomodoroTimer!.SessionCompleted += (_, _) => wasInvoked = true;

        // Act
        _pomodoroTimer.Start();
        await Task.Delay((int)_pomodoroTimer.SessionDuration.TotalMilliseconds + TestDelayMilliseconds);
        _pomodoroTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.IsRunning, Is.False, "Timer should not be running after session completion.");
            Assert.That(_pomodoroTimer.ElapsedTimeInSession, Is.EqualTo(_pomodoroTimer.SessionDuration), "Elapsed time in session should equal session duration.");
            Assert.That(_pomodoroTimer.CompletedWorkUnits, Is.EqualTo(TotalWorkUnits), "Completed work units should equal total work units.");
            Assert.That(wasInvoked, Is.True, $"{nameof(PomodoroTimer.SessionCompleted)} event was not invoked after the session completed.");
        });
    }

    [Test]
    public async Task Start_ShouldResetSession_AfterCompletedSession()
    {
        // Arrange
        var settings = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = 1,
            WorkUnitsBeforeLongBreak = 1
        };

        var pomodoroTimer = new PomodoroTimer(Substitute.For<Action<TimeSpan>>(), settings);

        // Act
        pomodoroTimer.Start();
        await Task.Delay((int)pomodoroTimer.SessionDuration.TotalMilliseconds + TestDelayMilliseconds);

        pomodoroTimer.Start(); // Restart the timer
        var elapsedTime = pomodoroTimer.ElapsedTimeInSession;
        var remainingTime = pomodoroTimer.RemainingTimeInSession;
        var currentPhase = pomodoroTimer.CurrentPhase;
        var completedWorkUnits = pomodoroTimer.CompletedWorkUnits;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(elapsedTime, Is.EqualTo(TimeSpan.Zero).Within(TestDelayMilliseconds).Milliseconds);
            Assert.That(remainingTime, Is.EqualTo(pomodoroTimer.SessionDuration).Within(TestDelayMilliseconds).Milliseconds);
            Assert.That(currentPhase, Is.EqualTo(PomodoroPhase.Work));
            Assert.That(completedWorkUnits, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Stop_ShouldStopTimer_WhenRunning()
    {
        // Arrange
        var wasPhaseCompletedInvoked = false;
        var wasSessionCompletedInvoked = false;
        var wasPhaseSkippedInvoked = false;

        _pomodoroTimer!.PhaseCompleted += (_, _) => wasPhaseCompletedInvoked = true;
        _pomodoroTimer.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;
        _pomodoroTimer.PhaseSkipped += (_, _) => wasPhaseSkippedInvoked = true;

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.IsRunning, Is.False,
                "Timer should not be running after Stop is called.");

            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time in current phase should not be reset after stopping.");

            Assert.That(_pomodoroTimer.ElapsedTimeInSession, Is.GreaterThan(TimeSpan.Zero),
                "Elapsed time in session should not be reset after stopping.");

            Assert.That(_pomodoroTimer.RemainingTimeInCurrentPhase.TotalSeconds, Is.LessThanOrEqualTo(_pomodoroTimer.Settings.WorkDurationSeconds),
                "Remaining time in current phase should be less than or equal to work duration.");

            Assert.That(wasPhaseSkippedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseSkipped)} event should not be invoked when stopping the timer.");

            Assert.That(wasPhaseCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked when stopping the timer.");

            Assert.That(wasSessionCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should not be invoked when stopping the timer.");
        });
    }

    [Test]
    public async Task Stop_ShouldDoNothing_WhenNotRunning()
    {
        // Arrange
        var initialElapsedTime = TimeSpan.Zero;

        // Act
        _pomodoroTimer!.Start();
        await Task.Delay(TestDelayMilliseconds);
        _pomodoroTimer.Stop();
        initialElapsedTime = _pomodoroTimer.ElapsedTimeInCurrentPhase;

        _pomodoroTimer.Stop(); // Attempt to stop again

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.IsRunning, Is.False,
                "Timer should not be running after Stop is called.");

            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase, Is.EqualTo(initialElapsedTime),
                "Elapsed time in current phase should not change when Stop is called while not running.");

            Assert.That(_pomodoroTimer.RemainingTimeInCurrentPhase.TotalSeconds, Is.EqualTo(_pomodoroTimer.Settings.WorkDurationSeconds - initialElapsedTime.TotalSeconds).Within(0.1).Percent,
                "Remaining time in current phase should not change when Stop is called while not running.");
        });
    }

    [Test]
    public async Task ResetSession_ShouldStopTheTimerAndResetAllTimersAndProperties()
    {
        // Arrange
        var settings = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = 2,
            WorkUnitsBeforeLongBreak = 2 // Set to 2 to ensure we have a short break after the first work unit
        };

        _pomodoroTimer = new PomodoroTimer(Substitute.For<Action<TimeSpan>>(), settings);

        var wasPhaseCompletedInvoked = false;
        var wasSessionCompletedInvoked = false;
        var wasPhaseSkippedInvoked = false;

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds + 1000 * _pomodoroTimer.Settings.WorkDurationSeconds); // wait for first work phase to complete

        // Subscribe to events just before resetting the session
        _pomodoroTimer.PhaseCompleted += (_, _) => wasPhaseCompletedInvoked = true;
        _pomodoroTimer.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;
        _pomodoroTimer.PhaseSkipped += (_, _) => wasPhaseSkippedInvoked = true;

        _pomodoroTimer.ResetSession();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.IsRunning, Is.False,
                "Timer should not be running after ResetSession.");

            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero),
                "Elapsed time in current phase should be reset to zero.");

            Assert.That(_pomodoroTimer.RemainingTimeInCurrentPhase.TotalSeconds, Is.EqualTo(_pomodoroTimer.Settings.WorkDurationSeconds),
                "Remaining time in current phase should equal work duration after reset.");

            Assert.That(_pomodoroTimer.ElapsedTimeInSession, Is.EqualTo(TimeSpan.Zero),
                "Elapsed time in session should be reset to zero.");

            Assert.That(_pomodoroTimer.RemainingTimeInSession, Is.EqualTo(_pomodoroTimer.SessionDuration),
                "Remaining time in session should equal session duration after reset.");

            Assert.That(_pomodoroTimer.CompletedWorkUnits, Is.EqualTo(0),
                "Completed work units should be reset to zero.");

            Assert.That(_pomodoroTimer.CurrentPhase, Is.EqualTo(PomodoroPhase.Work),
                "Current phase should be reset to Work.");

            Assert.That(wasPhaseCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked when resetting the session.");

            Assert.That(wasSessionCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should not be invoked when resetting the session.");

            Assert.That(wasPhaseSkippedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseSkipped)} event should not be invoked when resetting the session.");
        });
    }

    [Test]
    public async Task ResetCurrentPhase_ShouldStopTheTimerAndResetCurrentPhase()
    {
        // Arrange
        var settings = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = 2,
            WorkUnitsBeforeLongBreak = 2 // Set to 2 to ensure we have a short break after the first work unit
        };

        _pomodoroTimer = new PomodoroTimer(Substitute.For<Action<TimeSpan>>(), settings);

        var wasPhaseCompletedInvoked = false;
        var wasSessionCompletedInvoked = false;
        var wasPhaseSkippedInvoked = false;

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds + 1000 * _pomodoroTimer.Settings.WorkDurationSeconds); // wait for first work phase to complete

        // Subscribe to events just before resetting the current phase
        _pomodoroTimer.PhaseCompleted += (_, _) => wasPhaseCompletedInvoked = true;
        _pomodoroTimer.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;
        _pomodoroTimer.PhaseSkipped += (_, _) => wasPhaseSkippedInvoked = true;

        _pomodoroTimer.ResetCurrentPhase();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.IsRunning, Is.False,
                "Timer should not be running after ResetCurrentPhase.");

            Assert.That(_pomodoroTimer.CurrentPhase, Is.EqualTo(PomodoroPhase.ShortBreak),
                $"Current phase should remain {PomodoroPhase.ShortBreak} after resetting current phase.");

            Assert.That(_pomodoroTimer.CompletedWorkUnits, Is.EqualTo(1),
                "Completed work units should remain 1 after resetting current phase.");

            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero),
                "Elapsed time in current phase should be reset to zero.");

            Assert.That(_pomodoroTimer.RemainingTimeInCurrentPhase.TotalSeconds, Is.EqualTo(_pomodoroTimer.Settings.ShortBreakDurationSeconds),
                "Remaining time in current phase should equal short break duration after reset.");

            Assert.That(_pomodoroTimer.ElapsedTimeInSession.TotalSeconds, Is.EqualTo(_pomodoroTimer.Settings.WorkDurationSeconds),
                "Elapsed time in session should equal work duration after resetting current phase.");

            Assert.That(wasPhaseSkippedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseSkipped)} event should not be invoked when resetting the current phase.");

            Assert.That(wasPhaseCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked when resetting the current phase.");

            Assert.That(wasSessionCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should not be invoked when resetting the current phase.");
        });
    }

    [Test]
    public async Task SkipToNextPhase_ShouldSkipToNextPhaseAndKeepRunning_WhenRunning()
    {
        // Arrange
        var wasPhaseCompletedInvoked = false;
        var wasSessionCompletedInvoked = false;
        var wasPhaseSkippedInvoked = false;
        var isRunning = false;

        _pomodoroTimer!.PhaseCompleted += (_, _) => wasPhaseCompletedInvoked = true;
        _pomodoroTimer.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;
        _pomodoroTimer.PhaseSkipped += (_, _) => wasPhaseSkippedInvoked = true;

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds);

        _pomodoroTimer.SkipToNextPhase();
        await Task.Delay(TestDelayMilliseconds);

        isRunning = _pomodoroTimer.IsRunning;
        _pomodoroTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isRunning, Is.True,
                "Timer should still be running after skipping to next phase.");

            Assert.That(_pomodoroTimer.CurrentPhase, Is.Not.EqualTo(PomodoroPhase.Work),
                "Current phase should not be Work after skipping to next phase.");

            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase.TotalMilliseconds, Is.EqualTo(0.0).Within(2 * TestDelayMilliseconds),
                "Elapsed time in current phase should be reset to zero after skipping.");

            Assert.That(_pomodoroTimer.ElapsedTimeInSession.TotalMilliseconds, Is.EqualTo(1000 * WorkDurationSeconds).Within(2 * TestDelayMilliseconds),
                "Elapsed time in session should be greater than zero after skipping to next phase.");

            Assert.That(wasPhaseSkippedInvoked, Is.True,
                $"{nameof(PomodoroTimer.PhaseSkipped)} event should be invoked when skipping to next phase.");

            Assert.That(wasPhaseCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked when skipping to next phase.");

            Assert.That(wasSessionCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should not be invoked when skipping to next phase.");
        });
    }

    [Test]
    public void SkipToNextPhase_ShouldSkipToNextPhaseAndNotRun_WhenNotRunning()
    {
        // Arrange
        var wasPhaseCompletedInvoked = false;
        var wasSessionCompletedInvoked = false;
        var wasPhaseSkippedInvoked = false;
        var isRunning = false;

        _pomodoroTimer!.PhaseCompleted += (_, _) => wasPhaseCompletedInvoked = true;
        _pomodoroTimer.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;
        _pomodoroTimer.PhaseSkipped += (_, _) => wasPhaseSkippedInvoked = true;

        // Act
        _pomodoroTimer.SkipToNextPhase();
        isRunning = _pomodoroTimer.IsRunning;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isRunning, Is.False,
                "Timer should not be running after skipping to next phase when it wasn't started before.");

            Assert.That(_pomodoroTimer.CurrentPhase, Is.Not.EqualTo(PomodoroPhase.Work),
                "Current phase should not be Work after skipping to next phase.");

            Assert.That(_pomodoroTimer.ElapsedTimeInCurrentPhase, Is.EqualTo(TimeSpan.Zero),
                "Elapsed time in current phase should be reset to zero after skipping.");

            Assert.That(wasPhaseSkippedInvoked, Is.True,
                $"{nameof(PomodoroTimer.PhaseSkipped)} event should be invoked when skipping to next phase.");

            Assert.That(wasPhaseCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked when skipping to next phase.");

            Assert.That(wasSessionCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should not be invoked when skipping to next phase.");
        });
    }

    [Test]
    [TestCase(5, 19)]
    [TestCase(4, 20)]
    [TestCase(3, 20)]
    [TestCase(2, 21)]
    [TestCase(1, 23)]
    public void SessionDuration_ShouldReturnTotalDurationOfSession(int workUnitsBeforeLongBreak, int expectedDuration)
    {
        // Arrange
        var settings = new PomodoroSettings
        {
            WorkDurationSeconds = 3,
            ShortBreakDurationSeconds = 1,
            LongBreakDurationSeconds = 2,
            TotalWorkUnits = 5,
            WorkUnitsBeforeLongBreak = workUnitsBeforeLongBreak
        };
        _pomodoroTimer = new PomodoroTimer(Substitute.For<Action<TimeSpan>>(), settings);

        // Act
        var actualDuration = _pomodoroTimer.SessionDuration.TotalSeconds;

        // Assert
        Assert.That(actualDuration, Is.EqualTo(expectedDuration),
            "Session duration should equal the total duration of all work units and breaks.");
    }

    [Test]
    public void UpdateSettings_UpdatesTheSettings()
    {
        // Arrange
        var newSettings = new PomodoroSettings
        {
            WorkDurationSeconds = 5,
            ShortBreakDurationSeconds = 2,
            LongBreakDurationSeconds = 3,
            TotalWorkUnits = 5,
            WorkUnitsBeforeLongBreak = 3
        };

        var newSessionDuration = TimeSpan.FromSeconds(34); // 5 work units + 1 long break + 2 short breaks
        var wasSessionCompletedInvoked = false;
        var wasPhaseCompletedInvoked = false;
        var wasPhaseSkippedInvoked = false;

        _pomodoroTimer!.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;
        _pomodoroTimer.PhaseCompleted += (_, _) => wasPhaseCompletedInvoked = true;
        _pomodoroTimer.PhaseSkipped += (_, _) => wasPhaseSkippedInvoked = true;

        // Act
        _pomodoroTimer.UpdateSettings(newSettings);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_pomodoroTimer.Settings.WorkDurationSeconds, Is.EqualTo(newSettings.WorkDurationSeconds),
                $"{nameof(PomodoroSettings.WorkDurationSeconds)} should be updated.");

            Assert.That(_pomodoroTimer.Settings.ShortBreakDurationSeconds, Is.EqualTo(newSettings.ShortBreakDurationSeconds),
                $"{nameof(PomodoroSettings.ShortBreakDurationSeconds)} should be updated.");

            Assert.That(_pomodoroTimer.Settings.LongBreakDurationSeconds, Is.EqualTo(newSettings.LongBreakDurationSeconds),
                $"{nameof(PomodoroSettings.LongBreakDurationSeconds)} should be updated.");

            Assert.That(_pomodoroTimer.Settings.TotalWorkUnits, Is.EqualTo(newSettings.TotalWorkUnits),
                $"{nameof(PomodoroSettings.TotalWorkUnits)} should be updated.");

            Assert.That(_pomodoroTimer.Settings.WorkUnitsBeforeLongBreak, Is.EqualTo(newSettings.WorkUnitsBeforeLongBreak),
                $"{nameof(PomodoroSettings.WorkUnitsBeforeLongBreak)} should be updated.");

            Assert.That(_pomodoroTimer.SessionDuration, Is.EqualTo(newSessionDuration),
                "Session duration should match the new settings.");

            Assert.That(wasSessionCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should not be invoked.");

            Assert.That(wasPhaseCompletedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseCompleted)} event should not be invoked.");

            Assert.That(wasPhaseSkippedInvoked, Is.False,
                $"{nameof(PomodoroTimer.PhaseSkipped)} event should not be invoked.");

            Assert.That(_pomodoroTimer.IsRunning, Is.False,
                "Timer should not be running after updating settings if timer wasn't running before.");
        });
    }

    [Test]
    [TestCase(-1, 1, 1, 1, 1)]
    [TestCase(0, 1, 1, 1, 1)]
    [TestCase(1, -1, 1, 1, 1)]
    [TestCase(1, 0, 1, 1, 1)]
    [TestCase(1, 1, -1, 1, 1)]
    [TestCase(1, 2, 1, 1, 1)]
    [TestCase(1, 1, 1, -1, 1)]
    [TestCase(1, 1, 1, 0, 1)]
    [TestCase(1, 1, 1, 1, -1)]
    [TestCase(1, 1, 1, 1, 0)]
    public void UpdateSettings_ShouldThrow_WhenInvalidSettingsProvided(int workSeconds, int shortBreakSeconds, int longBreakSeconds, int totalWorkUnits, int workUnitsBeforeLongBreak)
    {
        // Arrange
        var invalidSettings = new PomodoroSettings
        {
            WorkDurationSeconds = workSeconds,
            ShortBreakDurationSeconds = shortBreakSeconds,
            LongBreakDurationSeconds = longBreakSeconds,
            TotalWorkUnits = totalWorkUnits,
            WorkUnitsBeforeLongBreak = workUnitsBeforeLongBreak
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _pomodoroTimer!.UpdateSettings(invalidSettings));
    }

    [Test]
    public async Task UpdateSettings_ShouldKeepTimerRunning_WhenRunning()
    {
        // Arrange
        var isRunning = false;
        var initialPhase = _pomodoroTimer!.CurrentPhase;

        var newSettings = new PomodoroSettings
        {
            WorkDurationSeconds = 5,
            ShortBreakDurationSeconds = 2,
            LongBreakDurationSeconds = 3,
            TotalWorkUnits = 5,
            WorkUnitsBeforeLongBreak = 3
        };

        // Act
        _pomodoroTimer!.Start();
        await Task.Delay(TestDelayMilliseconds);

        _pomodoroTimer.UpdateSettings(newSettings);
        isRunning = _pomodoroTimer.IsRunning;

        _pomodoroTimer.Stop();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isRunning, Is.True,
                "Timer should still be running after updating settings.");

            Assert.That(_pomodoroTimer.Settings.WorkDurationSeconds, Is.EqualTo(newSettings.WorkDurationSeconds),
                $"{nameof(PomodoroSettings.WorkDurationSeconds)} should be updated.");

            Assert.That(_pomodoroTimer.Settings.ShortBreakDurationSeconds, Is.EqualTo(newSettings.ShortBreakDurationSeconds),
                $"{nameof(PomodoroSettings.ShortBreakDurationSeconds)} should be updated.");

            Assert.That(_pomodoroTimer.Settings.LongBreakDurationSeconds, Is.EqualTo(newSettings.LongBreakDurationSeconds),
                $"{nameof(PomodoroSettings.LongBreakDurationSeconds)} should be updated.");

            Assert.That(_pomodoroTimer.Settings.TotalWorkUnits, Is.EqualTo(newSettings.TotalWorkUnits),
                $"{nameof(PomodoroSettings.TotalWorkUnits)} should be updated.");

            Assert.That(_pomodoroTimer.Settings.WorkUnitsBeforeLongBreak, Is.EqualTo(newSettings.WorkUnitsBeforeLongBreak),
                $"{nameof(PomodoroSettings.WorkUnitsBeforeLongBreak)} should be updated.");

            Assert.That(_pomodoroTimer.CurrentPhase, Is.EqualTo(initialPhase),
                "Current phase should remain the same with the provided new settings.");
        });
    }

    [Test]
    public async Task UpdateSettings_ShouldCompleteSession_WhenCurrentPhaseIsWorkAndNewTotalWorkUnitsLessThanCompletedWorkUnitsPlusCurrentWorkUnit()
    {
        // Arrange
        var wasSessionCompletedInvoked = false;

        var initialSettings = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = 3,
            WorkUnitsBeforeLongBreak = 3
        };

        _pomodoroTimer = new PomodoroTimer(Substitute.For<Action<TimeSpan>>(), initialSettings);
        _pomodoroTimer!.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;

        var secondsToWaitUntilThirdWorkPhaseBegins = 2 * WorkDurationSeconds + 2 * ShortBreakDurationSeconds;

        var newSettings = new PomodoroSettings
        {
            WorkDurationSeconds = WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = LongBreakDurationSeconds,
            TotalWorkUnits = 2, // One less than the work unit we will be in when the settings are updated
            WorkUnitsBeforeLongBreak = 3
        };

        var newSessionDuration = TimeSpan.FromSeconds(2 * WorkDurationSeconds + ShortBreakDurationSeconds);

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds + 1000 * secondsToWaitUntilThirdWorkPhaseBegins); // wait for last work phase to start

        _pomodoroTimer.UpdateSettings(newSettings);
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(wasSessionCompletedInvoked, Is.True,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should be invoked when the session is completed due to updated settings.");

            Assert.That(_pomodoroTimer.IsRunning, Is.False,
                "Timer should not be running after updating settings that complete the session.");

            Assert.That(_pomodoroTimer.CompletedWorkUnits, Is.EqualTo(2),
                $"Completed work units should equal new value of {nameof(PomodoroSettings.TotalWorkUnits)} after completing the session.");

            Assert.That(_pomodoroTimer.SessionDuration, Is.EqualTo(newSessionDuration),
                "Session duration should match the new settings after completing the session.");

            Assert.That(_pomodoroTimer.ElapsedTimeInSession, Is.EqualTo(newSessionDuration),
                "Elapsed time in session should equal new session duration after completing the session.");
        });
    }


    [Test]
    public async Task UpdateSettings_ShouldCompleteSession_WhenCurrentPhaseIsBreakAndNewTotalWorkUnitsLessThanCompletedWorkUnits()
    {
        // Arrange
        var wasSessionCompletedInvoked = false;

        var initialSettings = new PomodoroSettings
        {
            WorkDurationSeconds = 2 * WorkDurationSeconds,
            ShortBreakDurationSeconds = ShortBreakDurationSeconds,
            LongBreakDurationSeconds = ShortBreakDurationSeconds,
            TotalWorkUnits = 3,
            WorkUnitsBeforeLongBreak = 3
        };

        _pomodoroTimer = new PomodoroTimer(Substitute.For<Action<TimeSpan>>(), initialSettings);
        _pomodoroTimer!.SessionCompleted += (_, _) => wasSessionCompletedInvoked = true;

        var secondsToWaitUntilSecondBreakBegins = 2 * initialSettings.WorkDurationSeconds + initialSettings.ShortBreakDurationSeconds;

        var newSettings = new PomodoroSettings
        {
            WorkDurationSeconds = initialSettings.WorkDurationSeconds,
            ShortBreakDurationSeconds = initialSettings.ShortBreakDurationSeconds,
            LongBreakDurationSeconds = initialSettings.LongBreakDurationSeconds,
            TotalWorkUnits = initialSettings.TotalWorkUnits - 1, // One less than the work unit we will be in when the settings are updated
            WorkUnitsBeforeLongBreak = 3
        };

        var newSessionDuration = TimeSpan.FromSeconds(2 * newSettings.WorkDurationSeconds + newSettings.ShortBreakDurationSeconds);

        // Act
        _pomodoroTimer.Start();
        await Task.Delay(TestDelayMilliseconds + 1000 * secondsToWaitUntilSecondBreakBegins); // wait for last break to start

        _pomodoroTimer.UpdateSettings(newSettings);
        await Task.Delay(TestDelayMilliseconds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(wasSessionCompletedInvoked, Is.True,
                $"{nameof(PomodoroTimer.SessionCompleted)} event should be invoked when the session is completed due to updated settings.");

            Assert.That(_pomodoroTimer.IsRunning, Is.False,
                "Timer should not be running after updating settings that complete the session.");

            Assert.That(_pomodoroTimer.CompletedWorkUnits, Is.EqualTo(2),
                $"Completed work units should equal new value of {nameof(PomodoroSettings.TotalWorkUnits)} after completing the session.");

            Assert.That(_pomodoroTimer.SessionDuration, Is.EqualTo(newSessionDuration),
                "Session duration should match the new settings after completing the session.");

            Assert.That(_pomodoroTimer.ElapsedTimeInSession, Is.EqualTo(newSessionDuration),
                "Elapsed time in session should equal new session duration after completing the session.");
        });
    }
}
