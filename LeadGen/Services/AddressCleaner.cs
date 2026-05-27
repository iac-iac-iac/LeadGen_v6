using System.Text.RegularExpressions;

namespace LeadGen.Services;

/// <summary>
/// Очистка адреса по whitelist локаций (порт из Python-референса).
/// </summary>
public static partial class AddressCleaner
{
    private static readonly string[] AllowedLocations =
    [
        "Владивосток", "Екатеринбург", "МО", "Махачкала", "Новосибирск", "Омск", "Пермь",
        "Санкт-Петербург", "Уфа", "Ярославль", "Воронеж", "Иркутск", "Казань", "Калининград",
        "Кемерово", "Киров", "Краснодар", "Курск", "Лен. Обл.", "Липецк", "Москва",
        "Нижний Новгород", "Новокузнецк", "Оренбург", "Ростов-на-Дону", "Самара", "Саратов",
        "Сочи", "Тольятти", "Тюмень", "Чебоксары", "Челябинск", "Красноярск", "Пенза",
        "Тула", "Астрахань", "Барнаул", "Ижевск", "Томск", "Ульяновск", "Хабаровск", "Волгоград", "Рязань"
    ];

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["московская область"] = "МО",
        ["московская обл"] = "МО",
        ["московская обл."] = "МО",
        ["ленинградская область"] = "Лен. Обл.",
        ["ленинградская обл"] = "Лен. Обл.",
        ["ленинградская обл."] = "Лен. Обл.",
        ["лен. обл"] = "Лен. Обл.",
        ["спб"] = "Санкт-Петербург",
        ["санкт петербург"] = "Санкт-Петербург",
        ["нижний новгород"] = "Нижний Новгород",
        ["ростов на дону"] = "Ростов-на-Дону"
    };

    private static readonly HashSet<string> NormalizedAllowed = BuildNormalizedSet();

    public static string? CleanAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var kept = new List<string>();
        var hasLocation = false;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();

            if (IsAddressDetail(trimmed))
            {
                kept.Add(trimmed);
                continue;
            }

            var normalized = NormalizeFragment(trimmed);
            if (normalized is null)
                continue;

            if (IsAllowedLocation(normalized))
            {
                hasLocation = true;
                kept.Add(normalized);
            }
            else if (hasLocation)
            {
                kept.Add(trimmed);
            }
        }

        return hasLocation && kept.Count > 0 ? string.Join(", ", kept) : null;
    }

    private static HashSet<string> BuildNormalizedSet()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var loc in AllowedLocations)
        {
            set.Add(NormalizeKey(loc));
            set.Add(loc);
        }

        foreach (var alias in Aliases.Values)
            set.Add(NormalizeKey(alias));

        return set;
    }

    private static string? NormalizeFragment(string fragment)
    {
        var text = PrefixRegex().Replace(fragment.Trim(), string.Empty).Trim();
        if (string.IsNullOrEmpty(text))
            return null;

        var key = NormalizeKey(text);
        if (Aliases.TryGetValue(key, out var alias))
            return alias;

        foreach (var loc in AllowedLocations)
        {
            if (key == NormalizeKey(loc))
                return loc;
        }

        if (RegionJunkRegex().IsMatch(text))
            return null;

        return text;
    }

    private static bool IsAllowedLocation(string text) =>
        NormalizedAllowed.Contains(text) || NormalizedAllowed.Contains(NormalizeKey(text));

    private static bool IsAddressDetail(string fragment) =>
        AddressDetailRegex().IsMatch(fragment);

    private static string NormalizeKey(string text) =>
        text.ToLowerInvariant().Replace(".", string.Empty).Replace("  ", " ").Trim();

    [GeneratedRegex(@"^(?:г\.?|город|пос\.?|пгт\.?|дер\.?|с\.?)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex PrefixRegex();

    [GeneratedRegex(@"(?:автономный\s+округ|автономная\s+область|\b(?:республика|область|край)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex RegionJunkRegex();

    [GeneratedRegex(
        @"(?i)\bул\.?\b|\bулиц|\bпросп|\bшоссе\b|\bш\.?\b|\bпер\.?\b|\bпереул|\bбульв|\bб-р\b|\bнаб\.?\b|\bпл\.?\b|\bплощад|\bаллея\b|\bлиния\b|\bмкр|\bмикрорайон\b|\bд\.?\s*\d|\bдом\s*\d|\bкв\.?\s*\d|\bкорп\.?\s*\d|\bстр\.?\s*\d|^\d+[а-яa-z]?$",
        RegexOptions.IgnoreCase)]
    private static partial Regex AddressDetailRegex();
}
