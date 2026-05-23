using System.IO;
using LeadGen.Models;

namespace LeadGen.Services;

/// <summary>
/// Генератор ссылок Яндекс.Карт с per-city slug и region ID.
/// </summary>
public class LinkGeneratorService
{
    /// <summary>
    /// Генерирует ссылку с корректным slug города: /{id}/{slug}/search/{query}
    /// </summary>
    public static string GenerateLink(string segment, string region) =>
        YandexCityMap.BuildMapsUrl(segment, region);

    public List<GeneratedLink> GenerateBatch(string segment, IEnumerable<string> regions)
    {
        return regions.Select(r => new GeneratedLink
        {
            Segment = segment,
            Region = r,
            Link = GenerateLink(segment, r)
        }).ToList();
    }

    public IEnumerable<string> ExpandRegions(
        IEnumerable<string> selectedCities,
        Dictionary<string, List<string>> cityDistricts,
        bool includeDistricts)
    {
        foreach (var city in selectedCities)
        {
            yield return city;

            if (includeDistricts && cityDistricts.TryGetValue(city, out var districts))
            {
                foreach (var district in districts)
                    yield return $"{city} - {district}";
            }
        }
    }

    public void SaveToCsv(string filepath, IEnumerable<GeneratedLink> links)
    {
        using var writer = new StreamWriter(filepath, false, new System.Text.UTF8Encoding(true));
        writer.WriteLine("segment;region;link");

        foreach (var link in links)
            writer.WriteLine($"\"{link.Segment}\";\"{link.Region}\";\"{link.Link}\"");
    }
}
