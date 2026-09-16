using Jewel.JPMS.Api.Features.Documents;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>The words both renderers print — one spelling for Word and PDF alike.</summary>
internal static class ContractorsReportText
{
    public const string NoProgressRecorded = "No progress recorded.";
    public const string NothingToReport = "Nothing to report.";
    public const string NoLookAhead = "No items are planned for the coming week.";
    public const string NoDecisions = "No decisions or instructions are outstanding.";
    public const string NoVariations = "No variations are outstanding.";
    public const string NoBuildingControlCase = "No Building Control case is open on the project.";
    public const string NoSubcontractors = "No specialist subcontractors were on site this week.";
    public const string Yes = "Yes";
    public const string Dash = "—";

    public static string Date(DateOnly value) => value.ToString("d MMM yyyy", JewelDocumentStyle.Uk);
    public static string Date(DateOnly? value) => value is { } date ? Date(date) : Dash;
    public static string Money(decimal value) => JewelDocumentStyle.Money(value);
    public static string Period(ContractorsReportHeader header) => $"{Date(header.PeriodStart)} – {Date(header.PeriodEnd)}";
    public static string OrDash(string? text) => string.IsNullOrWhiteSpace(text) ? Dash : text.Trim();
    public static string OrNothingToReport(string text) => string.IsNullOrWhiteSpace(text) ? NothingToReport : text.Trim();
    public static string Days(int? days) => days is { } count ? count.ToString(JewelDocumentStyle.Uk) : Dash;

    public static string DecisionsCount(int count) => count == 1
        ? "1 RFI is open at the date of issue."
        : $"{count} RFIs are open at the date of issue.";

    public static string ValuationNumber(ContractorsReportHeader header) =>
        string.IsNullOrWhiteSpace(header.ValuationNumber) ? Dash : header.ValuationNumber;

    public static string Provenance(DateTimeOffset generatedAt) =>
        $"Generated {JewelDocumentStyle.DateAndTime(generatedAt)} · from the JPMS register (source of truth)";

    public static IReadOnlyList<ContractorsReportLookAheadItem> Planned(ContractorsReportDocument document) =>
        document.LookAhead.Where(item => !item.IsDone).ToList();

    public static IReadOnlyList<ContractorsReportDay> DaysWithPhotos(ContractorsReportDocument document) =>
        document.Progress.Where(day => day.Photos.Count > 0).ToList();
}
