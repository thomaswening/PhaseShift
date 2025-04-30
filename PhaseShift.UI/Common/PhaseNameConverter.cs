using System.Windows.Data;
using System.Windows;
using PhaseShift.Core;

namespace PhaseShift.UI.Common;

internal class PhaseNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not PomodoroPhase phase)
        {
            return DependencyProperty.UnsetValue;
        }

        return phase switch
        {
            PomodoroPhase.Work => "Work",
            PomodoroPhase.ShortBreak => "Short Break",
            PomodoroPhase.LongBreak => "Long Break",
            _ => DependencyProperty.UnsetValue,
        };
    }

    public object ConvertBack(object value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
