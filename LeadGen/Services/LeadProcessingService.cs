using System.Diagnostics;
using System.IO;
using System.Text.Json;
using LeadGen.Models;

namespace LeadGen.Services;

/// <summary>
/// Пайплайн обработки лидов из Webbee AI (JSON/TSV/CSV).
/// </summary>
public class LeadProcessingService
{
    private static readonly Dictionary<string, string> JsonColumnMap = new()
    {
        ["Название"] = "title",
        ["Адрес"] = "address",
        ["phone_1"] = "phone_1",
        ["phone_2"] = "phone_2",
        ["Category 0"] = "Category 0",
        ["companyUrl"] = "companyUrl",
        ["vkontakte"] = "vkontakte",
        ["telegram"] = "telegram"
    };

    public ProcessingResult ProcessFiles(
        IEnumerable<string> filePaths,
        IEnumerable<string> managers,
        ProcessingSettings settings)
    {
        var sw = Stopwatch.StartNew();
        var managerList = managers.Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        var allRows = new List<RawLeadRow>();
        var filesProcessed = 0;
        var removedExcludedCategory = 0;

        foreach (var path in filePaths)
        {
            var rows = LoadFile(path, settings, out var excludedInFile);
            removedExcludedCategory += excludedInFile;
            if (rows is null)
                continue;

            var sourceName = Path.GetFileName(path);
            foreach (var row in rows)
                row.PhoneSource = sourceName;

            allRows.AddRange(rows);
            filesProcessed++;
        }

        var totalRows = allRows.Count;
        var dupByPhone = 0;
        var dupByName = 0;

        if (settings.RemoveDuplicates)
        {
            var before = allRows.Count;
            allRows = RemoveDuplicatesByPhone(allRows, out dupByPhone);
            allRows = RemoveDuplicatesByName(allRows, out dupByName);
        }

        var leads = new List<LeadRecord>();
        var removedNoLocation = 0;
        var managerIndex = 0;

        foreach (var row in allRows)
        {
            var address = AddressCleaner.CleanAddress(row.Address);
            if (address is null)
            {
                removedNoLocation++;
                continue;
            }

            var leadTitle = !string.IsNullOrWhiteSpace(row.Category0) && !string.IsNullOrWhiteSpace(row.Name)
                ? $"{row.Category0} - {row.Name}"
                : row.Name;

            var manager = managerList.Count > 0
                ? managerList[managerIndex++ % managerList.Count]
                : "Не назначен";

            leads.Add(new LeadRecord
            {
                LeadTitle = leadTitle,
                WorkPhone = row.Phone1Clean ?? string.Empty,
                MobilePhone = row.Phone2Clean ?? string.Empty,
                Address = address,
                Website = row.Website ?? string.Empty,
                Telegram = row.Telegram ?? string.Empty,
                Vk = row.Vk ?? string.Empty,
                CompanyName = row.Name,
                PhoneSource = row.PhoneSource,
                Manager = manager
            });
        }

        sw.Stop();

        return new ProcessingResult
        {
            Leads = leads,
            FilesProcessed = filesProcessed,
            TotalRows = totalRows,
            DuplicatesRemoved = dupByPhone + dupByName,
            DuplicatesByPhone = dupByPhone,
            DuplicatesByName = dupByName,
            RowsRemovedNoLocation = removedNoLocation,
            RowsRemovedExcludedCategory = removedExcludedCategory,
            ProcessingTimeMs = sw.ElapsedMilliseconds
        };
    }

    private List<RawLeadRow> LoadFile(string filepath, ProcessingSettings settings, out int excludedByCategory)
    {
        excludedByCategory = 0;
        var ext = Path.GetExtension(filepath).ToLowerInvariant();

        return ext switch
        {
            ".json" => LoadJson(filepath, settings, out excludedByCategory) ?? [],
            ".tsv" => LoadDelimited(filepath, '\t', settings),
            ".csv" => LoadDelimited(filepath, ',', settings),
            _ => []
        };
    }

    private List<RawLeadRow>? LoadJson(string filepath, ProcessingSettings settings, out int excludedByCategory)
    {
        excludedByCategory = 0;
        var json = File.ReadAllText(filepath);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return null;

        var rows = new List<RawLeadRow>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in item.EnumerateObject())
                dict[prop.Name] = prop.Value.ToString();

            if (ExcludedCategories.ContainsExcludedCategory(dict))
            {
                excludedByCategory++;
                continue;
            }

            rows.Add(MapRow(dict, isJson: true, settings));
        }

        return rows;
    }

    private List<RawLeadRow> LoadDelimited(string filepath, char separator, ProcessingSettings settings)
    {
        var lines = File.ReadAllLines(filepath);
        if (lines.Length < 2)
            return [];

        var headers = lines[0].Split(separator);
        var rows = new List<RawLeadRow>();

        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var values = lines[i].Split(separator);
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var j = 0; j < headers.Length && j < values.Length; j++)
                dict[headers[j].Trim()] = values[j].Trim();

            rows.Add(MapRow(dict, isJson: false, settings));
        }

        return rows;
    }

    private RawLeadRow MapRow(Dictionary<string, string?> dict, bool isJson, ProcessingSettings settings)
    {
        string? Get(string key, string? jsonKey = null)
        {
            if (dict.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val;

            if (isJson && jsonKey is not null && dict.TryGetValue(jsonKey, out val))
                return val;

            return null;
        }

        var phone1 = PhoneValidator.CleanPhone(Get("phone_1"), settings.PhoneFormat, settings.MinPhoneLength);
        var phone2 = settings.IgnorePhone2
            ? null
            : PhoneValidator.CleanPhone(Get("phone_2"), settings.PhoneFormat, settings.MinPhoneLength);

        var name = Get("Название", "title") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || (phone1 is null && phone2 is null))
            return new RawLeadRow();

        return new RawLeadRow
        {
            Name = name,
            Address = Get("Адрес", "address"),
            Category0 = Get("Category 0"),
            Website = Get("companyUrl"),
            Telegram = Get("telegram"),
            Vk = Get("vkontakte"),
            Phone1Clean = phone1,
            Phone2Clean = phone2
        };
    }

    private static List<RawLeadRow> RemoveDuplicatesByPhone(List<RawLeadRow> rows, out int removed)
    {
        var seen = new HashSet<string>();
        var result = new List<RawLeadRow>();
        removed = 0;

        foreach (var row in rows)
        {
            var key = $"{row.Phone1Clean}|{row.Phone2Clean}";
            if (string.IsNullOrEmpty(row.Phone1Clean) && string.IsNullOrEmpty(row.Phone2Clean))
            {
                result.Add(row);
                continue;
            }

            if (seen.Add(key))
                result.Add(row);
            else
                removed++;
        }

        return result;
    }

    private static List<RawLeadRow> RemoveDuplicatesByName(List<RawLeadRow> rows, out int removed)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<RawLeadRow>();
        removed = 0;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                result.Add(row);
                continue;
            }

            if (seen.Add(row.Name))
                result.Add(row);
            else
                removed++;
        }

        return result;
    }

    private class RawLeadRow
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Category0 { get; set; }
        public string? Website { get; set; }
        public string? Telegram { get; set; }
        public string? Vk { get; set; }
        public string? Phone1Clean { get; set; }
        public string? Phone2Clean { get; set; }
        public string PhoneSource { get; set; } = string.Empty;
    }
}
