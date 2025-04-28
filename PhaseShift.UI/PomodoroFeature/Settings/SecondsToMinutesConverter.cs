using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PhaseShift.UI.PomodoroFeature.Settings;

internal class SecondsToMinutesConverter : IValueConverter
{
    private const int SecondsPerMinute = 60;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int seconds)
        {
            return DependencyProperty.UnsetValue;
        }

        var minutes = seconds / SecondsPerMinute;
        var secondsRemainder = seconds % SecondsPerMinute;

        return secondsRemainder == 0 ? minutes : Math.Round(seconds / 60.0, 1);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double minutes)
        {
            return DependencyProperty.UnsetValue;
        }

        var seconds = (int)(minutes * SecondsPerMinute);
        return seconds;
    }
}