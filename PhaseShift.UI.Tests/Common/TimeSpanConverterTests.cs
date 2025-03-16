using System.Globalization;
using System.Windows;

using NUnit.Framework;

using PhaseShift.UI.Common;

namespace PhaseShift.UI.Tests.Common;

[TestFixture]
internal class TimeSpanConverterTests
{
    private TimeSpanConverter? _converter;

    [SetUp]
    public void SetUp()
    {
        _converter = new TimeSpanConverter();
    }

    [Test]
    public void Convert_ValidTimeSpan_ReturnsFormattedString()
    {
        var timeSpan = new TimeSpan(1, 2, 3, 4);
        var result = _converter!.Convert(timeSpan, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(result, Is.EqualTo("1d 02:03:04"));
    }

    [Test]
    public void Convert_InvalidValue_ReturnsUnsetValue()
    {
        var result = _converter!.Convert("invalid", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(result, Is.EqualTo(DependencyProperty.UnsetValue));
    }

    [Test]
    public void ConvertBack_ValidString_ReturnsTimeSpan()
    {
        var timeSpanString = "02:03:04";
        var result = _converter!.ConvertBack(timeSpanString, typeof(TimeSpan), null, CultureInfo.InvariantCulture);
        Assert.That(result, Is.EqualTo(new TimeSpan(2, 3, 4)));
    }

    [Test]
    public void ConvertBack_InvalidString_ReturnsUnsetValue()
    {
        var result = _converter!.ConvertBack("invalid", typeof(TimeSpan), null, CultureInfo.InvariantCulture);
        Assert.That(result, Is.EqualTo(DependencyProperty.UnsetValue));
    }
}
