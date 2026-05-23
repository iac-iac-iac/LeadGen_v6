namespace LeadGen.Services;

/// <summary>
/// Yandex Maps: region ID + slug для каждого города.
/// Формат URL: https://yandex.ru/maps/{id}/{slug}/search/{query}
/// </summary>
public static class YandexCityMap
{
    public record CityInfo(int RegionId, string Slug);

    private static readonly Dictionary<string, CityInfo> Cities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Москва"] = new(213, "moscow"),
        ["Санкт-Петербург"] = new(2, "saint-petersburg"),
        ["Екатеринбург"] = new(54, "yekaterinburg"),
        ["Новосибирск"] = new(65, "novosibirsk"),
        ["Казань"] = new(43, "kazan"),
        ["Нижний Новгород"] = new(47, "nizhny-novgorod"),
        ["Челябинск"] = new(56, "chelyabinsk"),
        ["Самара"] = new(51, "samara"),
        ["Омск"] = new(66, "omsk"),
        ["Ростов-на-Дону"] = new(39, "rostov-na-donu"),
        ["Уфа"] = new(172, "ufa"),
        ["Красноярск"] = new(62, "krasnoyarsk"),
        ["Пермь"] = new(50, "perm"),
        ["Воронеж"] = new(193, "voronezh"),
        ["Волгоград"] = new(38, "volgograd"),
        ["Краснодар"] = new(35, "krasnodar"),
        ["Саратов"] = new(194, "saratov"),
        ["Тюмень"] = new(55, "tyumen"),
        ["Тольятти"] = new(240, "tolyatti"),
        ["Ижевск"] = new(44, "izhevsk"),
        ["Барнаул"] = new(197, "barnaul"),
        ["Ульяновск"] = new(195, "ulyanovsk"),
        ["Иркутск"] = new(63, "irkutsk"),
        ["Хабаровск"] = new(76, "khabarovsk"),
        ["Ярославль"] = new(16, "yaroslavl"),
        ["Владивосток"] = new(75, "vladivostok"),
        ["Махачкала"] = new(28, "makhachkala"),
        ["Томск"] = new(67, "tomsk"),
        ["Оренбург"] = new(48, "orenburg"),
        ["Кемерово"] = new(64, "kemerovo"),
        ["Новокузнецк"] = new(237, "novokuznetsk"),
        ["Рязань"] = new(11, "ryazan"),
        ["Астрахань"] = new(37, "astrakhan"),
        ["Пенза"] = new(49, "penza"),
        ["Липецк"] = new(9, "lipetsk"),
        ["Киров"] = new(46, "kirov"),
        ["Чебоксары"] = new(45, "cheboksary"),
        ["Тула"] = new(15, "tula"),
        ["Калининград"] = new(22, "kaliningrad"),
        ["Балашиха"] = new(10716, "balashikha"),
        ["Курск"] = new(8, "kursk"),
        ["Севастополь"] = new(959, "sevastopol"),
        ["Сочи"] = new(239, "sochi"),
    };

    private static readonly CityInfo Default = Cities["Москва"];

    /// <summary>
    /// Определяет город из строки региона (в т.ч. «Москва - ЦАО»).
    /// </summary>
    public static CityInfo Resolve(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
            return Default;

        // Точное совпадение
        if (Cities.TryGetValue(region.Trim(), out var info))
            return info;

        // Район: «Город - Район»
        var dashIdx = region.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIdx > 0)
        {
            var city = region[..dashIdx].Trim();
            if (Cities.TryGetValue(city, out info))
                return info;
        }

        // Частичное совпадение (самое длинное имя города)
        CityInfo? best = null;
        var bestLen = 0;
        foreach (var (name, cityInfo) in Cities)
        {
            if (region.StartsWith(name, StringComparison.OrdinalIgnoreCase) && name.Length > bestLen)
            {
                best = cityInfo;
                bestLen = name.Length;
            }
        }

        return best ?? Default;
    }

    public static string BuildMapsUrl(string segment, string region)
    {
        var info = Resolve(region);
        var query = Uri.EscapeDataString($"{segment} {region}");
        return $"https://yandex.ru/maps/{info.RegionId}/{info.Slug}/search/{query}";
    }
}
