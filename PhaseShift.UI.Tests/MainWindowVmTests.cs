using NUnit.Framework;

using PhaseShift.UI.PomodoroFeature;
using PhaseShift.UI.StopwatchFeature;
using PhaseShift.UI.Tests.Mocks;
using PhaseShift.UI.TimerFeature;

namespace PhaseShift.UI.Tests;

[TestFixture]
internal class MainWindowVmTests
{
    private MainWindowVm? _mainWindowVm;

    [SetUp]
    public void SetUp()
    {
        var dispatcher = new MockDispatcher();
        _mainWindowVm = new MainWindowVm(dispatcher);
    }

    [Test]
    public void Constructor_ShouldSetCurrentViewModelToTimerCollectionVm()
    {
        // Assert
        Assert.That(_mainWindowVm!.CurrentViewModel.GetType(), Is.EqualTo(typeof(PomodoroNavigationVm)));
    }

    [Test]
    public void ShowStopwatch_ShouldSetCurrentViewModelToStopwatchVm()
    {
        // Act
        _mainWindowVm!.ShowStopwatchCommand.Execute(null);

        // Assert
        Assert.That(_mainWindowVm!.CurrentViewModel.GetType(), Is.EqualTo(typeof(StopwatchVm)));
    }

    [Test]
    public void ShowTimers_ShouldSetCurrentViewModelToTimerCollectionVm()
    {
        // Arrange
        _mainWindowVm!.ShowStopwatchCommand.Execute(null);

        // Act
        _mainWindowVm!.ShowTimersCommand.Execute(null);

        // Assert
        Assert.That(_mainWindowVm!.CurrentViewModel.GetType(), Is.EqualTo(typeof(TimerCollectionVm)));
    }

    [Test]
    public void ShowPomodoroTimer_ShouldSetCurrentViewModelToPomodoroNavigationVm()
    {
        // Arrange
        _mainWindowVm!.ShowStopwatchCommand.Execute(null);
        
        // Act
        _mainWindowVm!.ShowPomodoroTimerCommand.Execute(null);

        // Assert
        Assert.That(_mainWindowVm!.CurrentViewModel.GetType(), Is.EqualTo(typeof(PomodoroNavigationVm)));
    }
}
