using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace PhaseShift.UI.Common;

/// <summary>
/// A converter that converts <see cref="TimeSpan"/> to string and vice versa.
/// Example input: <c>new TimeSpan(1, 2, 3, 4)</c> will be converted to <c>"1d 02:03:04"</c>.
/// </summary>
internal class TimeSpanConverter : IValueConverter
{
    private const string DefaultTimeFormat = @"hh\:mm\:ss";

    public object Convert(object value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not TimeSpan timeSpan)
        {
            return DependencyProperty.UnsetValue;
        }

        var format = parameter as string ?? DefaultTimeFormat;

        try
        {
            var timeSpanStr = timeSpan.ToString(format, culture);
            if (timeSpan.Days > 0)
            {
                timeSpanStr = $"{timeSpan.Days}d {timeSpanStr}";
            }

            return timeSpanStr;
        }
        catch (Exception)
        {
            return timeSpan.ToString(DefaultTimeFormat, culture);
        }
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not string timeSpanString
            || !TimeSpan.TryParse(timeSpanString, out TimeSpan timeSpan))
        {
            return DependencyProperty.UnsetValue;
        }

        return timeSpan;
    }
}
