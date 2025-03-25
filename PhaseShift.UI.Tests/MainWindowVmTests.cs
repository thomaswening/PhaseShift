using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

using NUnit.Framework;

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
        Assert.That(_mainWindowVm!.CurrentViewModel.GetType(), Is.EqualTo(typeof(TimerCollectionVm)));
    }

    [Test]
    public void ShowStopwatch_ShouldSetCurrentViewModelToStopwatchVm()
    {
        // Act
        _mainWindowVm!.ShowStopwatch();

        // Assert
        Assert.That(_mainWindowVm!.CurrentViewModel.GetType(), Is.EqualTo(typeof(StopwatchVm)));
    }

    [Test]
    public void ShowTimers_ShouldSetCurrentViewModelToTimerCollectionVm()
    {
        // Arrange
        _mainWindowVm!.ShowStopwatch();

        // Act
        _mainWindowVm!.ShowTimers();

        // Assert
        Assert.That(_mainWindowVm!.CurrentViewModel.GetType(), Is.EqualTo(typeof(TimerCollectionVm)));
    }
}
