using Jewel.JPMS.Api.Features.Documents;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>The PDF's own words; the sentences the page's preview shares are
/// <see cref="ContractorsReportPrintedText"/> and <see cref="ContractorsReportVariationWording"/>.</summary>
internal static class ContractorsReportText
{
    public const string NothingToReport = "Nothing to report.";
    public const string NoLookAhead = "No items are planned for the coming week.";
    public const string NoDecisions = "No decisions or instructions are outstanding.";
    public const string NoBuildingControlCase = "No Building Control case is open on the project.";
    public const string NoSubcontractors = "No specialist subcontractors were on site this week.";
    public const string SubcontractorsOpening =
        "Works during the period were undertaken by Jewel Bespoke Build's site team and general building trades, "
        + "supported by the specialist subcontractors set out below.";
    public const string Dash = "—";

    public static string Date(DateOnly value) => value.ToString("d MMM yyyy", JewelDocumentStyle.Uk);
    public static string Date(DateOnly? value) => value is { } date ? Date(date) : Dash;
    public static string OrDash(string? text) => string.IsNullOrWhiteSpace(text) ? Dash : text.Trim();
    public static string OrNothingToReport(string text) => string.IsNullOrWhiteSpace(text) ? NothingToReport : text.Trim();

    public static string BuildingControlContact(string contact) => $"Building Control Contact: {contact}";

    public static string PhotographCount(IReadOnlyList<ContractorsReportDay> days)
    {
        var total = days.Sum(day => day.Photos.Count);
        var dayWord = days.Count == 1 ? "day" : "days";
        return total == 1 ? "1 photograph." : $"{total} photographs across {days.Count} {dayWord}.";
    }

    public static string ValuationNumber(ContractorsReportHeader header) =>
        string.IsNullOrWhiteSpace(header.ValuationNumber) ? Dash : header.ValuationNumber;

    public static IReadOnlyList<ContractorsReportLookAheadItem> Planned(ContractorsReportDocument document) =>
        document.LookAhead.Where(item => !item.IsDone).ToList();

    public static IReadOnlyList<ContractorsReportDay> DaysWithPhotos(ContractorsReportDocument document) =>
        document.Progress.Where(day => day.Photos.Count > 0).ToList();
}
