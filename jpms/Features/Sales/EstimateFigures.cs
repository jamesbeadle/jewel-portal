using System.Globalization;

namespace Jewel.JPMS.Features.Sales;

/// <summary>How the estimate form reads what was typed: money and dates are held as text until
/// Save, so a half-typed figure never throws, and the one "not a number" answer sits next to
/// the field (FormField Error), never in a Notice.</summary>
public static class EstimateFigures
{
    public static decimal? ParseMoney(string text) =>
        decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;

    public static DateOnly? ParseDate(string text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", out var value) ? value : null;

    /// <summary>Null when the text is blank or a number — the field's inline error otherwise.</summary>
    public static string? MoneyError(string text) =>
        string.IsNullOrWhiteSpace(text) || ParseMoney(text) is not null ? null : "Not a number.";

    public static string MoneyText(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    public static string DateInputText(DateOnly? value) => value?.ToString("yyyy-MM-dd") ?? "";
}
