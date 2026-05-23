using System.Globalization;
using System.Text.RegularExpressions;

namespace LeadGen.Services;

/// <summary>
/// Нормализация телефонных номеров (порт из Python-референса).
/// </summary>
public static partial class PhoneValidator
{
    private static readonly HashSet<string> NanLike = new(StringComparer.OrdinalIgnoreCase)
    {
        "nan", "none", "", "null"
    };

    public static string? CleanPhone(object? phone, string phoneFormat = "7", int minLength = 10)
    {
        if (phone is null)
            return null;

        var phoneStr = phone.ToString()?.Trim() ?? string.Empty;
        if (NanLike.Contains(phoneStr))
            return null;

        // Научная нотация из Excel/pandas
        if (double.TryParse(phoneStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var phoneFloat)
            && phoneFloat != 0)
        {
            phoneStr = ((long)phoneFloat).ToString(CultureInfo.InvariantCulture);
        }

        var digits = NonDigitRegex().Replace(phoneStr, string.Empty);
        if (digits.Length < minLength)
            return null;

        if (digits.StartsWith('9') && digits.Length == 10)
            digits = "7" + digits;
        else if (digits.StartsWith('8') && digits.Length == 11)
            digits = "7" + digits[1..];

        if (!digits.StartsWith('7') || digits.Length is < 10 or > 11)
            return null;

        return FormatPhone(digits, phoneFormat);
    }

    public static string FormatPhone(string phone, string phoneFormat)
    {
        var baseNumber = phone.StartsWith('7') ? phone[1..] : phone;

        return phoneFormat switch
        {
            "8" => "8" + baseNumber,
            "+7" => "+7" + baseNumber,
            _ => "7" + baseNumber
        };
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();
}
