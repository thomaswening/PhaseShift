using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using PhaseShift.Core;
using PhaseShift.UI.Common;

namespace PhaseShift.UI.PomodoroFeature.Settings;

internal partial class PomodoroSettingsVm : PageViewModel
{
    private PomodoroSettings _settings = new();
    public override string Title => "Pomodoro Settings";

    [ObservableProperty]
    private int _longBreakDurationSeconds;

    [ObservableProperty]
    private int _shortBreakDurationSeconds;

    [ObservableProperty]
    private int _totalWorkUnits;

    [ObservableProperty]
    private int _totalWorkUnitsBeforeLongBreak;

    [ObservableProperty]
    private int _workDurationSeconds;

    public PomodoroSettingsVm()
    {
        LoadDefaultSettings();
    }

    public event EventHandler? TimerRequested;
    public event EventHandler<PomodoroSettings>? SettingsChanged;

    [RelayCommand]
    private void CancelSettings()
    {
        TimerRequested?.Invoke(this, EventArgs.Empty);
    }

    private void LoadDefaultSettings()
    {
        _settings = new PomodoroSettings();

        WorkDurationSeconds = _settings.WorkDurationSeconds;
        ShortBreakDurationSeconds = _settings.ShortBreakDurationSeconds;
        LongBreakDurationSeconds = _settings.LongBreakDurationSeconds;
        TotalWorkUnits = _settings.TotalWorkUnits;
        TotalWorkUnitsBeforeLongBreak = _settings.WorkUnitsBeforeLongBreak;
    }

    [RelayCommand]
    private void ResetSettingsToDefault()
    {
        LoadDefaultSettings();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settings.WorkDurationSeconds = WorkDurationSeconds;
        _settings.ShortBreakDurationSeconds = ShortBreakDurationSeconds;
        _settings.LongBreakDurationSeconds = LongBreakDurationSeconds;
        _settings.TotalWorkUnits = TotalWorkUnits;
        _settings.WorkUnitsBeforeLongBreak = TotalWorkUnitsBeforeLongBreak;

        SettingsChanged?.Invoke(this, _settings);
        TimerRequested?.Invoke(this, EventArgs.Empty);
    }
}
