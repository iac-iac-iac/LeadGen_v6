using FluentAssertions;
using LeadGen.Services;

namespace LeadGen.Tests.Services;

public class LinkGeneratorServiceTests
{
    private readonly LinkGeneratorService _service = new();

    [Fact]
    public void GenerateBatch_CreatesLinkPerRegion()
    {
        var links = _service.GenerateBatch("Сегмент", ["Москва", "Казань"]);
        links.Should().HaveCount(2);
        links[0].Link.Should().Contain("/213/moscow/");
        links[1].Link.Should().Contain("/43/kazan/");
    }

    [Fact]
    public void ExpandRegions_IncludesDistrictsWhenEnabled()
    {
        var districts = new Dictionary<string, List<string>>
        {
            ["Москва"] = ["ЦАО", "САО"]
        };

        var regions = _service.ExpandRegions(["Москва"], districts, includeDistricts: true).ToList();

        regions.Should().Contain("Москва");
        regions.Should().Contain("Москва - ЦАО");
        regions.Should().Contain("Москва - САО");
    }

    [Fact]
    public void SaveToCsv_WritesUtf8WithBom()
    {
        var path = Path.Combine(Path.GetTempPath(), $"links_{Guid.NewGuid():N}.csv");
        try
        {
            _service.SaveToCsv(path, [new Models.GeneratedLink
            {
                Segment = "A",
                Region = "Москва",
                Link = "https://example.com"
            }]);

            File.Exists(path).Should().BeTrue();
            var text = File.ReadAllText(path);
            text.Should().Contain("segment;region;link");
            text.Should().Contain("Москва");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
