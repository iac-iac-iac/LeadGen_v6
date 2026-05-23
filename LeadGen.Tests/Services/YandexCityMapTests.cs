using FluentAssertions;
using LeadGen.Services;

namespace LeadGen.Tests.Services;

public class YandexCityMapTests
{
    [Theory]
    [InlineData("Москва", 213, "moscow")]
    [InlineData("Санкт-Петербург", 2, "saint-petersburg")]
    [InlineData("Казань", 43, "kazan")]
    public void Resolve_ReturnsCorrectCityInfo(string city, int id, string slug)
    {
        var info = YandexCityMap.Resolve(city);
        info.RegionId.Should().Be(id);
        info.Slug.Should().Be(slug);
    }

    [Fact]
    public void Resolve_DistrictUsesParentCity()
    {
        var info = YandexCityMap.Resolve("Москва - ЦАО");
        info.RegionId.Should().Be(213);
        info.Slug.Should().Be("moscow");
    }

    [Fact]
    public void BuildMapsUrl_UsesCitySlug()
    {
        var url = YandexCityMap.BuildMapsUrl("Металлоконструкции", "Казань");
        url.Should().StartWith("https://yandex.ru/maps/43/kazan/search/");
        url.Should().Contain("%D0%9C%D0%B5%D1%82%D0%B0%D0%BB%D0%BB"); // URL-encoded query
    }

    [Fact]
    public void BuildMapsUrl_UnknownCityFallsBackToMoscow()
    {
        var url = YandexCityMap.BuildMapsUrl("test", "Неизвестный");
        url.Should().Contain("/213/moscow/search/");
    }
}
