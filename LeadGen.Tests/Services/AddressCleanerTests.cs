using FluentAssertions;
using LeadGen.Services;

namespace LeadGen.Tests.Services;

public class AddressCleanerTests
{
    [Fact]
    public void CleanAddress_MoscowOblast_KeepsStreet()
    {
        var result = AddressCleaner.CleanAddress("Московская область, ул. Ленина, д. 5");
        result.Should().Be("МО, ул. Ленина, д. 5");
    }

    [Fact]
    public void CleanAddress_MoscowOblastShortForm_KeepsStreet()
    {
        var result = AddressCleaner.CleanAddress("Московская обл., улица Пушкина, 12");
        result.Should().Be("МО, улица Пушкина, 12");
    }

    [Fact]
    public void CleanAddress_MoskovskyProspekt_NotCollapsedToMoOnly()
    {
        var result = AddressCleaner.CleanAddress("Московская область, Московский проспект, 25");
        result.Should().Be("МО, Московский проспект, 25");
    }

    [Fact]
    public void CleanAddress_MoscowCity_StillWorks()
    {
        var result = AddressCleaner.CleanAddress("Москва, ул. Ленина, д. 1");
        result.Should().Be("Москва, ул. Ленина, д. 1");
    }
}
