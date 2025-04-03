using System.Windows;
using System.Windows.Data;

namespace PhaseShift.UI.TimerFeature;

internal class TimeSpanToDateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not TimeSpan timeSpan)
        {
            return DependencyProperty.UnsetValue;
        }

        return DateTime.Today + timeSpan;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not DateTime dateTime)
        {
            return DependencyProperty.UnsetValue;
        }

        return dateTime.TimeOfDay;        
    }
}
