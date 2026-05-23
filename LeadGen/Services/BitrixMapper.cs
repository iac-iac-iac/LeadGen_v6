using System.Globalization;
using System.IO;
using System.Text;
using LeadGen.Models;

namespace LeadGen.Services;

/// <summary>
/// Маппинг в формат Bitrix24 (64 колонки, разделитель ;).
/// </summary>
public static class BitrixMapper
{
    public static readonly string[] BitrixColumns =
    [
        "ID", "Название лида", "Обращение", "Имя", "Фамилия", "Отчество", "Имя, Фамилия",
        "Дата рождения", "Адрес", "Улица, номер дома", "Квартира, офис, комната, этаж",
        "Населенный пункт", "Район", "Регион", "Почтовый индекс", "Страна",
        "Рабочий телефон", "Мобильный телефон", "Номер факса", "Домашний телефон",
        "Номер пейджера", "Телефон для рассылок", "Другой телефон", "Корпоративный сайт",
        "Личная страница", "Страница Facebook", "Страница ВКонтакте", "Страница LiveJournal",
        "Микроблог Twitter", "Другой сайт", "Рабочий e-mail", "Частный e-mail",
        "E-mail для рассылок", "Другой e-mail", "Контакт Facebook", "Контакт Telegram",
        "Контакт ВКонтакте", "Контакт Viber", "Комментарии Instagram",
        "Контакт Битрикс24 Network", "Онлайн-чат", "Контакт Открытая линия", "Другой контакт",
        "Связанный пользователь", "Название компании", "Должность", "Комментарий", "Стадия",
        "Дополнительно о стадии", "Товар", "Цена", "Количество", "Возможная сумма", "Валюта",
        "Источник", "Дополнительно об источнике", "Доступен для всех", "Ответственный",
        "Тип услуги", "Новый файл", "Доп файл", "Источник телефона", "Причина отказа", "дело"
    ];

    public static List<Dictionary<string, string>> MapToBitrix(
        IEnumerable<LeadRecord> leads,
        BitrixSettings settings)
    {
        var result = new List<Dictionary<string, string>>();

        foreach (var lead in leads)
        {
            if (string.IsNullOrWhiteSpace(lead.LeadTitle)
                && string.IsNullOrWhiteSpace(lead.WorkPhone)
                && string.IsNullOrWhiteSpace(lead.MobilePhone)
                && string.IsNullOrWhiteSpace(lead.Address)
                && string.IsNullOrWhiteSpace(lead.CompanyName))
            {
                continue;
            }

            var row = BitrixColumns.ToDictionary(c => c, _ => string.Empty);

            row["Название лида"] = lead.LeadTitle;
            row["Название компании"] = lead.CompanyName;
            row["Рабочий телефон"] = lead.WorkPhone;
            row["Мобильный телефон"] = lead.MobilePhone;
            row["Адрес"] = lead.Address;
            row["Корпоративный сайт"] = CleanUrl(lead.Website);
            row["Контакт Telegram"] = CleanTelegram(lead.Telegram);
            row["Страница ВКонтакте"] = CleanVk(lead.Vk);
            row["Стадия"] = settings.Stage;
            row["Источник"] = settings.Source;
            row["Доступен для всех"] = "да";
            row["Ответственный"] = lead.Manager;
            row["Тип услуги"] = settings.ServiceType;
            row["Источник телефона"] = lead.PhoneSource;

            result.Add(row);
        }

        return result;
    }

    public static void ExportToCsv(string filepath, List<Dictionary<string, string>> rows)
    {
        using var writer = new StreamWriter(filepath, false, new UTF8Encoding(true));

        writer.WriteLine(string.Join(";", BitrixColumns.Select(Quote)));

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(";", BitrixColumns.Select(c => Quote(row.GetValueOrDefault(c, string.Empty)))));
        }
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static string CleanUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var idx = url.IndexOf('?');
        return idx >= 0 ? url[..idx] : url;
    }

    private static string CleanTelegram(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var cleaned = value.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("t.me/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("@", "", StringComparison.OrdinalIgnoreCase)
            .Split('/')[0];

        return string.IsNullOrEmpty(cleaned) ? string.Empty : "@" + cleaned;
    }

    private static string CleanVk(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("vk.com/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("@", "", StringComparison.OrdinalIgnoreCase)
            .Split('/')[0];
    }
}
