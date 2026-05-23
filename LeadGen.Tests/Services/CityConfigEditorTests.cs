using FluentAssertions;
using LeadGen.Models;
using LeadGen.Services;

namespace LeadGen.Tests.Services;

public class CityConfigEditorTests
{
    [Fact]
    public void RestoreDefaults_Restores43Cities()
    {
        var config = new AppConfig { Regions = ["Test"] };
        CityConfigEditor.RestoreDefaults(config);

        config.Regions.Should().HaveCount(43);
        config.CityDistricts.Should().ContainKey("Москва");
        config.CityDistricts["Санкт-Петербург"].Should().HaveCount(18);
    }

    [Fact]
    public void AddCity_RequiresNameAndTimezone()
    {
        var config = new AppConfig();
        CityConfigEditor.AddCity(config, "", "UTC+3", out var error).Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AddDistrict_AddsToCity()
    {
        var config = new AppConfig
        {
            Regions = ["Москва"],
            CityDistricts = new Dictionary<string, List<string>>()
        };
        CityConfigEditor.AddDistrict(config, "Москва", "Тестовый район", out _).Should().BeTrue();
        config.CityDistricts["Москва"].Should().Contain("Тестовый район");
    }

    [Fact]
    public void RemoveCities_AlsoRemovesDistricts()
    {
        var config = new AppConfig
        {
            Regions = ["Москва", "Казань"],
            CityDistricts = new Dictionary<string, List<string>> { ["Москва"] = ["ЦАО"] }
        };

        CityConfigEditor.RemoveCities(config, ["Москва"]);

        config.Regions.Should().BeEquivalentTo(["Казань"]);
        config.CityDistricts.Should().NotContainKey("Москва");
    }
}
