using System.Globalization;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Section 4's sentences as issued Report 30 printed them — one spelling for the page's
/// preview, the Word and the PDF.</summary>
public static class ContractorsReportVariationWording
{
    public const string Opening =
        "The following variations remain at status “Issued” on the variation register and are unanswered. "
        + "Formal response / AI is required to support the next valuation and programme update.";

    public const string NoneOutstanding = "No variations are outstanding.";

    private const string LongDateFormat = "d MMMM yyyy";
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    public static string Row(ContractorsReportVariation variation) => $"{variation.DisplayNumber} — {variation.Title}";

    public static string Position(ContractorsReportVariation variation) => variation.IssuedOn is { } issuedOn
        ? $"Issued {LongDate(issuedOn)} — no response received"
        : "Issued — no response received";

    /// <summary>"V78 and V80 were approved on 12 September 2026 and are no longer outstanding." —
    /// null when nothing was approved in the period.</summary>
    public static string? ApprovedSince(IReadOnlyList<ContractorsReportApprovedVariation> approved)
    {
        if (approved.Count == 0) return null;
        var isOne = approved.Count == 1;
        var dates = approved.Select(variation => variation.ApprovedOn).Distinct().ToList();
        var verb = isOne ? "was" : "were";
        var standing = isOne ? "is" : "are";
        if (dates.Count == 1)
        {
            var names = Joined(approved.Select(variation => variation.DisplayNumber).ToList());
            return $"{names} {verb} approved on {LongDate(dates[0])} and {standing} no longer outstanding.";
        }
        var dated = Joined(approved.Select(variation => $"{variation.DisplayNumber} ({LongDate(variation.ApprovedOn)})").ToList());
        return $"{dated} were approved in the period and are no longer outstanding.";
    }

    public static string Paragraph(ContractorsReportDocument document) => WithApproved(Opening, document);

    public static string NothingOutstanding(ContractorsReportDocument document) => WithApproved(NoneOutstanding, document);

    private static string WithApproved(string sentence, ContractorsReportDocument document) =>
        ApprovedSince(document.VariationsApproved) is { } approved ? $"{sentence} {approved}" : sentence;

    private static string LongDate(DateOnly date) => date.ToString(LongDateFormat, Uk);

    private static string Joined(IReadOnlyList<string> items) => items.Count switch
    {
        1 => items[0],
        _ => $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}"
    };
}
