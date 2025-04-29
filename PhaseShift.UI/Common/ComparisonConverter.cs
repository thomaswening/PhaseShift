using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace PhaseShift.UI.Common;

internal class ComparisonConverter : IValueConverter
{
    public ComparisonOperator Operator { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null
            || parameter is null
            || value is not IComparable comparableValue
            || !TryConvertParameter(value, parameter, culture, out var comparableParameter))
        {
            return DependencyProperty.UnsetValue;
        }

        var comparisonResult = comparableValue.CompareTo(comparableParameter);

        return Operator switch
        {
            ComparisonOperator.Equal => comparisonResult == 0,
            ComparisonOperator.NotEqual => comparisonResult != 0,
            ComparisonOperator.GreaterThan => comparisonResult > 0,
            ComparisonOperator.LessThan => comparisonResult < 0,
            ComparisonOperator.GreaterThanOrEqual => comparisonResult >= 0,
            ComparisonOperator.LessThanOrEqual => comparisonResult <= 0,
            _ => throw new InvalidOperationException("Invalid comparison operator."),
        };
    }

    private static bool TryConvertParameter(object value, object parameter, CultureInfo culture, [NotNullWhen(true)] out IComparable? comparableParameter)
    {
        try
        {
            comparableParameter = System.Convert.ChangeType(parameter, value.GetType(), culture) as IComparable;
            return comparableParameter is not null;
        }
        catch (Exception)
        {
            comparableParameter = null;
            return false;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual
}
