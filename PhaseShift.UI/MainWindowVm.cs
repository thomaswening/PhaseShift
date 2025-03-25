using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using PhaseShift.UI.Common;
using PhaseShift.UI.StopwatchFeature;
using PhaseShift.UI.TimerFeature;

namespace PhaseShift.UI;

internal partial class MainWindowVm : ObservableObject
{
    private readonly Dictionary<Type, PageViewModel> _viewModels;

    [ObservableProperty]
    private PageViewModel _currentViewModel;

    public MainWindowVm() : this(new DispatcherWrapper(System.Windows.Application.Current.Dispatcher)) { }

    public MainWindowVm(IDispatcher dispatcher)
    {
        var stopwatchVm = new StopwatchVm(dispatcher);
        var timerCollectionVm = new TimerCollectionVm(dispatcher);
        timerCollectionVm.TimerCompleted += (s, e) => TimerCompleted?.Invoke(s, e);

        _viewModels = new Dictionary<Type, PageViewModel>
        {
            { typeof(TimerCollectionVm), timerCollectionVm },
            { typeof(StopwatchVm), stopwatchVm },
        };

        CurrentViewModel = _viewModels[typeof(TimerCollectionVm)];
    }

    public event EventHandler<TimerCompletedEventArgs>? TimerCompleted;

    [RelayCommand]
    public void ShowStopwatch()
    {
        CurrentViewModel = _viewModels[typeof(StopwatchVm)];
    }

    [RelayCommand]
    public void ShowTimers()
    {
        CurrentViewModel = _viewModels[typeof(TimerCollectionVm)];
    }
}
