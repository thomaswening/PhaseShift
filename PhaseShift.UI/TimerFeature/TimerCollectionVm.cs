using System.Collections.ObjectModel;
using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using PhaseShift.UI.Common;

namespace PhaseShift.UI.TimerFeature;

internal partial class TimerCollectionVm : PageViewModel
{
    private readonly IDispatcher _dispatcher;
    public override string Title => "Timers";

    public ObservableCollection<TimerVm> Timers { get; } = [];
    public event EventHandler<TimerCompletedEventArgs>? TimerCompleted;

    [ObservableProperty]
    private TimeSpan newTimerDuration = TimeSpan.FromSeconds(10);

    [ObservableProperty]
    private int _activeTimersCount;

    [ObservableProperty]
    private TimerVm? _nextDueTimer;

    public TimerCollectionVm() : this(new DispatcherWrapper(System.Windows.Application.Current.Dispatcher)) { } // for designer
    public TimerCollectionVm(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        AddTimer();
    }

    [RelayCommand]
    public void AddTimer()
    {
        var timer = new TimerVm(NewTimerDuration, _dispatcher);
        timer.TimerCompleted += (s, e) => TimerCompleted?.Invoke(s, e);
        timer.DeleteTimerRequested += (_, _) => Timers.Remove(timer);
        timer.PropertyChanged += OnTimerChanged;

        Timers.Add(timer);
    }

    private void OnTimerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimerVm.IsRunning))
        {
            UpdateTimerCollectionStatus();
        }
    }

    private void UpdateTimerCollectionStatus()
    {
        var activeTimers = Timers.Where(t => t.IsRunning).ToList();
        ActiveTimersCount = activeTimers.Count;

        UpdateNextDueTimer(activeTimers);
    }

    private void UpdateNextDueTimer(List<TimerVm> activeTimers)
    {
        var nextDueTimer = activeTimers.OrderBy(t => t.RemainingTime).FirstOrDefault();
        if (nextDueTimer is null || nextDueTimer.RemainingTime < TimeSpan.Zero)
        {
            NextDueTimer = null;
            return;
        }

        NextDueTimer = nextDueTimer;
    }
}
