using FluentAssertions;
using LeadGen.Models;
using LeadGen.Services;

namespace LeadGen.Tests.Services;

public class PhoneValidatorTests
{
    [Theory]
    [InlineData("79001234567", "7", "79001234567")]
    [InlineData("89001234567", "7", "79001234567")]
    [InlineData("9001234567", "7", "79001234567")]
    [InlineData("79001234567", "+7", "+79001234567")]
    public void CleanPhone_NormalizesRussianNumbers(string input, string format, string expected)
    {
        PhoneValidator.CleanPhone(input, format, 10).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    public void CleanPhone_ReturnsNullForInvalid(object? input)
    {
        PhoneValidator.CleanPhone(input).Should().BeNull();
    }

    [Fact]
    public void CleanPhone_HandlesScientificNotation()
    {
        PhoneValidator.CleanPhone(7.9001234567e10).Should().Be("79001234567");
    }
}
