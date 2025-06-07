using NUnit.Framework;

using PhaseShift.Core;
using PhaseShift.UI.PomodoroFeature;
using PhaseShift.UI.Tests.Mocks;

namespace PhaseShift.UI.Tests.PomodoroFeature;

[TestFixture]
internal class PomodoroNavigationVmTests
{
    private static readonly PomodoroSettings TestSettings = new()
    {
        WorkDurationSeconds = 1,
        ShortBreakDurationSeconds = 1,
        LongBreakDurationSeconds = 1,
        TotalWorkUnits = 2,
        WorkUnitsBeforeLongBreak = 1
    };

    private PomodoroNavigationVm? _navigationVm;

    [SetUp]
    public void SetUp()
    {
        _navigationVm = new PomodoroNavigationVm(new MockDispatcher());
        _navigationVm.TimerVm.UpdateSettings(TestSettings);
    }

    [Test]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_navigationVm!.CurrentViewModel, Is.EqualTo(_navigationVm.TimerVm));
            Assert.That(_navigationVm.Title, Is.EqualTo(_navigationVm.TimerVm.Title));
        });
    }

    [Test]
    public void OnPomodoroSettingsRequested_SwitchesToSettingsViewModel()
    {
        // Act
        _navigationVm!.TimerVm.EditPomodoroSettingsCommand.Execute(null);

        // Assert
        Assert.That(_navigationVm.CurrentViewModel, Is.EqualTo(_navigationVm.SettingsVm));
    }

    [Test]
    public void OnTimerRequested_SwitchesBackToTimerViewModel()
    {
        // Arrange
        _navigationVm!.TimerVm.EditPomodoroSettingsCommand.Execute(null);

        // Act
        _navigationVm.SettingsVm.CancelSettingsCommand.Execute(null);

        // Assert
        Assert.That(_navigationVm.CurrentViewModel, Is.EqualTo(_navigationVm.TimerVm));
    }

    [Test]
    public void OnSettingsChanged_UpdatesTimerSettings()
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
        _navigationVm!.TimerVm.UpdateSettings(newSettings);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_navigationVm.TimerVm.TotalWorkUnits, Is.EqualTo(newSettings.TotalWorkUnits));
            Assert.That(_navigationVm.TimerVm.WorkUnitsBeforeLongBreak, Is.EqualTo(newSettings.WorkUnitsBeforeLongBreak));
            Assert.That(_navigationVm.TimerVm.ShortBreakEqualsLongBreak, Is.EqualTo(newSettings.LongBreakDurationSeconds == newSettings.ShortBreakDurationSeconds));
        });
    }

    [Test]
    public async Task OnActiveTimerCompleted_InvokesTimerCompletedEvent()
    {
        // Arrange
        bool eventInvoked = false;
        _navigationVm!.TimerCompleted += (_, _) => eventInvoked = true;

        // Act
        _navigationVm.TimerVm.StartTimerCommand.Execute(null);
        await Task.Delay((int)_navigationVm.TimerVm.SessionDuration.TotalMilliseconds + 200);

        // Assert
        Assert.That(eventInvoked, Is.True);
    }

    [Test]
    public void OnPropertyChanged_InvokesStatusChangedEvent_WhenRelevantPropertiesChange()
    {
        // Arrange
        bool eventInvoked = false;
        _navigationVm!.StatusChanged += (_, _) => eventInvoked = true;

        // Act
        _navigationVm.TimerVm.IsRunning = true;

        // Assert
        Assert.That(eventInvoked, Is.True);
    }

    [Test]
    public void Title_ReflectsCurrentViewModelTitle()
    {
        // Arrange
        var initialTitle = _navigationVm!.Title;

        // Act
        _navigationVm.TimerVm.EditPomodoroSettingsCommand.Execute(null);
        var updatedTitle = _navigationVm.Title;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(initialTitle, Is.EqualTo(_navigationVm.TimerVm.Title));
            Assert.That(updatedTitle, Is.Not.EqualTo(initialTitle));
        });
    }
}
