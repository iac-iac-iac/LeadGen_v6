namespace LeadGen.Services;

/// <summary>
/// Часовые пояса городов для отображения в UI.
/// </summary>
public static class CityTimezones
{
    private static readonly Dictionary<string, string> DefaultTimezones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Калининград"] = "UTC+2",
        ["Москва"] = "UTC+3",
        ["Санкт-Петербург"] = "UTC+3",
        ["Казань"] = "UTC+3",
        ["Нижний Новгород"] = "UTC+3",
        ["Воронеж"] = "UTC+3",
        ["Краснодар"] = "UTC+3",
        ["Ростов-на-Дону"] = "UTC+3",
        ["Сочи"] = "UTC+3",
        ["Самара"] = "UTC+4",
        ["Ижевск"] = "UTC+4",
        ["Ульяновск"] = "UTC+4",
        ["Саратов"] = "UTC+4",
        ["Астрахань"] = "UTC+4",
        ["Екатеринбург"] = "UTC+5",
        ["Пермь"] = "UTC+5",
        ["Челябинск"] = "UTC+5",
        ["Оренбург"] = "UTC+5",
        ["Уфа"] = "UTC+5",
        ["Омск"] = "UTC+6",
        ["Красноярск"] = "UTC+7",
        ["Новосибирск"] = "UTC+7",
        ["Кемерово"] = "UTC+7",
        ["Новокузнецк"] = "UTC+7",
        ["Томск"] = "UTC+7",
        ["Иркутск"] = "UTC+8",
        ["Якутск"] = "UTC+9",
        ["Владивосток"] = "UTC+10",
        ["Хабаровск"] = "UTC+10",
        ["Магадан"] = "UTC+11",
        ["Петропавловск-Камчатский"] = "UTC+12"
    };

    public static string? GetTimezone(string city, Dictionary<string, string>? custom = null)
    {
        if (custom?.TryGetValue(city, out var tz) == true)
            return tz;

        if (DefaultTimezones.TryGetValue(city, out tz))
            return tz;

        foreach (var (name, offset) in DefaultTimezones)
        {
            if (city.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                return offset;
        }

        return null;
    }
}
