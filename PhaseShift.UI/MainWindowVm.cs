using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using PhaseShift.UI.Common;
using PhaseShift.UI.PomodoroFeature;
using PhaseShift.UI.StatusOverview;
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

        var pomodoroNavigationVm = new PomodoroNavigationVm(dispatcher);
        pomodoroNavigationVm.TimerCompleted += (s, e) => TimerCompleted?.Invoke(s, e);

        _viewModels = new Dictionary<Type, PageViewModel>
        {
            { typeof(TimerCollectionVm), timerCollectionVm },
            { typeof(StopwatchVm), stopwatchVm },
            { typeof(PomodoroNavigationVm), pomodoroNavigationVm }
        };

        CurrentViewModel = _viewModels[typeof(PomodoroNavigationVm)];
        StatusVm = new StatusVm(pomodoroNavigationVm.TimerVm, timerCollectionVm, stopwatchVm, CurrentViewModel);
        PropertyChanged += OnPropertyChanged;
    }

    public event EventHandler<TimerCompletedEventArgs>? TimerCompleted;
    public StatusVm StatusVm { get; init; }

    [RelayCommand]
    private void ShowPomodoroTimer()
    {
        CurrentViewModel = _viewModels[typeof(PomodoroNavigationVm)];
    }

    [RelayCommand]
    private void ShowStopwatch()
    {
        CurrentViewModel = _viewModels[typeof(StopwatchVm)];
    }

    [RelayCommand]
    private void ShowTimers()
    {
        CurrentViewModel = _viewModels[typeof(TimerCollectionVm)];
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentViewModel))
        {
            StatusVm.SelectedVm = CurrentViewModel;
        }
    }
}
