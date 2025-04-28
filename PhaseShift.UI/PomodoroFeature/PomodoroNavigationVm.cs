using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;

using PhaseShift.Core;
using PhaseShift.UI.Common;

using PhaseShift.UI.PomodoroFeature.Settings;

namespace PhaseShift.UI.PomodoroFeature;

internal partial class PomodoroNavigationVm : PageViewModel
{
    public PomodoroTimerVm TimerVm { get; private set; }
    public PomodoroSettingsVm SettingsVm { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private PageViewModel _currentViewModel;

    public override string Title => CurrentViewModel.Title;

    public event EventHandler<PomodoroTimerCompletedEventArgs>? TimerCompleted;
    public event EventHandler? StatusChanged;

    public PomodoroNavigationVm(IDispatcher? dispatcher = null)
    {
        dispatcher ??= new DispatcherWrapper(System.Windows.Application.Current.Dispatcher);

        var timerVm = CreatePomodoroTimerVm(dispatcher);
        TimerVm = timerVm;

        SettingsVm = new PomodoroSettingsVm();
        SettingsVm.TimerRequested += OnTimerRequested;
        SettingsVm.SettingsChanged += OnSettingsChanged;

        CurrentViewModel = TimerVm;
    }

    private PomodoroTimerVm CreatePomodoroTimerVm(IDispatcher dispatcher, PomodoroSettings? settings = null)
    {
        var timerVm = new PomodoroTimerVm(settings, dispatcher);
        timerVm.ActiveTimerCompleted += OnActiveTimerCompleted;
        timerVm.PomodoroSettingsRequested += OnPomodoroSettingsRequested;
        timerVm.PropertyChanged += OnPropertyChanged;
        return timerVm;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PomodoroTimerVm.IsRunning)
            || e.PropertyName == nameof(PomodoroTimerVm.CurrentPhase)
            || e.PropertyName == nameof(PomodoroTimerVm.RemainingTime))
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnActiveTimerCompleted(object? sender, PomodoroTimerCompletedEventArgs e)
    {
        TimerCompleted?.Invoke(sender, e);
    }

    private void OnSettingsChanged(object? sender, PomodoroSettings settings)
    {
        TimerVm.UpdateSettings(settings);
    }

    private void OnTimerRequested(object? sender, EventArgs e)
    {
        CurrentViewModel = TimerVm;
    }

    private void OnPomodoroSettingsRequested(object? sender, EventArgs e)
    {
        CurrentViewModel = SettingsVm;
    }
}
