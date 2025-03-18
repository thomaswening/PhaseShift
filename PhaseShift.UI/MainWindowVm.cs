using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using PhaseShift.UI.Common;
using PhaseShift.UI.StopwatchFeature;

namespace PhaseShift.UI;

internal partial class MainWindowVm : ObservableObject
{
    private readonly Dictionary<Type, PageViewModel> _viewModels;

    [ObservableProperty]
    private PageViewModel _currentViewModel;

    public MainWindowVm() : this(new DispatcherWrapper(Application.Current.Dispatcher)) { }

    public MainWindowVm(IDispatcher dispatcher)
    {
        var stopwatchVm = new StopwatchVm(dispatcher);

        _viewModels = new Dictionary<Type, PageViewModel>
        {
            { typeof(StopwatchVm), stopwatchVm },
        };

        CurrentViewModel = _viewModels[typeof(StopwatchVm)];
    }

    [RelayCommand]
    public void ShowStopwatch()
    {
        CurrentViewModel = _viewModels[typeof(StopwatchVm)];
    }
}
