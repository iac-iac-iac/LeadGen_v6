using System.IO;
using System.Text.Json.Serialization;

namespace LeadGen.Models;

/// <summary>
/// Конфигурация приложения (config.json).
/// </summary>
public class AppConfig
{
    public List<string> Managers { get; set; } = [];

    public ProcessingSettings Processing { get; set; } = new();

    public PathSettings Paths { get; set; } = new();

    public BitrixSettings Bitrix { get; set; } = new();

    public List<string> Regions { get; set; } = DefaultRegions.All;

    public Dictionary<string, List<string>> CityDistricts { get; set; } = DefaultRegions.Districts;

    public Dictionary<string, string> CityTimezones { get; set; } = new();

    public UiSettings Ui { get; set; } = new();
}

public class UiSettings
{
    [JsonPropertyName("animations_enabled")]
    public bool AnimationsEnabled { get; set; } = true;
}

public class ProcessingSettings
{
    [JsonPropertyName("phone_format")]
    public string PhoneFormat { get; set; } = "7";

    [JsonPropertyName("remove_duplicates")]
    public bool RemoveDuplicates { get; set; } = true;

    [JsonPropertyName("min_phone_length")]
    public int MinPhoneLength { get; set; } = 10;

    [JsonPropertyName("ignore_phone_2")]
    public bool IgnorePhone2 { get; set; }
}

public class PathSettings
{
    [JsonPropertyName("input_dir")]
    public string InputDir { get; set; } = "data/input";

    [JsonPropertyName("output_dir")]
    public string OutputDir { get; set; } = "data/output";

    [JsonPropertyName("database")]
    public string Database { get; set; } = "data/database.db";

    /// <summary>
    /// Resolves a path relative to the application base directory when not rooted.
    /// </summary>
    public static string Resolve(string baseDirectory, string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path);
}

public class BitrixSettings
{
    public string Stage { get; set; } = "Новая заявка";
    public string Source { get; set; } = "Холодный звонок";

    [JsonPropertyName("service_type")]
    public string ServiceType { get; set; } = "ГЦК";
}

public static class DefaultRegions
{
    public static readonly List<string> All =
    [
        "Москва", "Санкт-Петербург", "Екатеринбург", "Новосибирск", "Казань",
        "Нижний Новгород", "Челябинск", "Самара", "Омск", "Ростов-на-Дону",
        "Уфа", "Красноярск", "Пермь", "Воронеж", "Волгоград", "Краснодар",
        "Саратов", "Тюмень", "Тольятти", "Ижевск", "Барнаул", "Ульяновск",
        "Иркутск", "Хабаровск", "Ярославль", "Владивосток", "Махачкала",
        "Томск", "Оренбург", "Кемерово", "Новокузнецк", "Рязань", "Астрахань",
        "Пенза", "Липецк", "Киров", "Чебоксары", "Тула", "Калининград",
        "Балашиха", "Курск", "Севастополь", "Сочи"
    ];

    public static readonly Dictionary<string, List<string>> Districts = new()
    {
        ["Москва"] =
        [
            "ЦАО", "САО", "СВАО", "ВАО", "ЮВАО", "ЮАО", "ЮЗАО", "ЗАО", "СЗАО",
            "Зеленоград", "Троицкий АО", "Новомосковский АО"
        ],
        ["Санкт-Петербург"] =
        [
            "Адмиралтейский", "Василеостровский", "Выборгский", "Калининский",
            "Кировский", "Колпинский", "Красногвардейский", "Красносельский",
            "Кронштадтский", "Курортный", "Московский", "Невский", "Петроградский",
            "Петродворцовый", "Приморский", "Пушкинский", "Фрунзенский", "Центральный"
        ]
    };
}
