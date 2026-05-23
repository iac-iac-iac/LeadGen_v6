using FluentAssertions;
using LeadGen.Helpers;
using System.Windows.Data;

namespace LeadGen.Tests.Helpers;

public class EnumEqualsConverterTests
{
    private readonly EnumEqualsConverter _converter = new();

    [Theory]
    [InlineData("Dashboard", "Dashboard", true)]
    [InlineData("Processing", "Dashboard", false)]
    public void Convert_ComparesEnumToParameter(string enumValue, string parameter, bool expected)
    {
        var result = _converter.Convert(enumValue, typeof(bool), parameter, System.Globalization.CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertBack_ReturnsDoNothing_DoesNotThrow()
    {
        var act = () => _converter.ConvertBack(true, typeof(object), "Dashboard", System.Globalization.CultureInfo.InvariantCulture);
        act.Should().NotThrow().Which.Should().Be(Binding.DoNothing);
    }
}
