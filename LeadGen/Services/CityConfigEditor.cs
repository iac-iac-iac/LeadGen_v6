using LeadGen.Models;

namespace LeadGen.Services;

/// <summary>
/// Операции с городами и районами в config.json.
/// </summary>
public static class CityConfigEditor
{
    public static void RestoreDefaults(AppConfig config)
    {
        config.Regions = [.. DefaultRegions.All];
        config.CityDistricts = DefaultRegions.Districts.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToList());
        config.CityTimezones.Clear();
    }

    public static bool AddCity(AppConfig config, string name, string timezone, out string? error)
    {
        error = null;
        name = name.Trim();
        timezone = timezone.Trim();

        if (string.IsNullOrEmpty(name))
        {
            error = "Введите название города";
            return false;
        }

        if (string.IsNullOrEmpty(timezone))
        {
            error = "Введите часовой пояс (UTC+X)";
            return false;
        }

        if (config.Regions.Contains(name))
        {
            error = $"Город «{name}» уже существует";
            return false;
        }

        config.Regions.Add(name);
        config.CityTimezones[name] = timezone;
        return true;
    }

    public static bool UpdateCityTimezone(AppConfig config, string city, string timezone, out string? error)
    {
        error = null;
        timezone = timezone.Trim();

        if (string.IsNullOrEmpty(timezone))
        {
            error = "Введите часовой пояс (UTC+X)";
            return false;
        }

        if (!config.Regions.Contains(city))
        {
            error = "Город не найден";
            return false;
        }

        config.CityTimezones[city] = timezone;
        return true;
    }

    public static void RemoveCities(AppConfig config, IEnumerable<string> cityNames)
    {
        foreach (var city in cityNames.ToList())
        {
            config.Regions.Remove(city);
            config.CityDistricts.Remove(city);
            config.CityTimezones.Remove(city);
        }
    }

    public static bool AddDistrict(AppConfig config, string city, string district, out string? error)
    {
        error = null;
        district = district.Trim();

        if (string.IsNullOrEmpty(district))
        {
            error = "Введите название района";
            return false;
        }

        if (!config.Regions.Contains(city))
        {
            error = "Сначала выберите город";
            return false;
        }

        if (!config.CityDistricts.TryGetValue(city, out var list))
        {
            list = [];
            config.CityDistricts[city] = list;
        }

        if (list.Contains(district))
        {
            error = $"Район «{district}» уже существует";
            return false;
        }

        list.Add(district);
        return true;
    }

    public static void RemoveDistricts(AppConfig config, string city, IEnumerable<string> districts)
    {
        if (!config.CityDistricts.TryGetValue(city, out var list))
            return;

        foreach (var d in districts.ToList())
            list.Remove(d);

        if (list.Count == 0)
            config.CityDistricts.Remove(city);
    }

    public static string GetTimezone(AppConfig config, string city) =>
        CityTimezones.GetTimezone(city, config.CityTimezones) ?? "UTC+3";
}
