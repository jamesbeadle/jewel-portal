using System.Globalization;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// The sentences the Contractor's Report prints as issued Report 30 words them (Jeremy's review of
/// Report 31, 24 Sep 2026) — one spelling for the PDF and the page's preview.
/// </summary>
public static class ContractorsReportPrintedText
{
    public const string ProgressOpening = "Works progressed during the period as set out below.";
    public const string InstructionsHeading = "Instructions and confirmations received this period";
    public const string DecisionsOpening =
        "The following items remain open on the RFI register and are required to support continued progress. "
        + "Formal responses are requested from PLG.";
    public const string AwaitingResponse = "Awaiting response";

    private const string LongDateFormat = "d MMMM yyyy";
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");
    private static readonly char[] BulletMarks = { '-', '•', '*', '–' };

    /// <summary>"1. Progress Against Programme, PoW Rev 9" — the programme reference beside the title.</summary>
    public static string ProgressHeading(string sectionTitle, string programmeReference) =>
        string.IsNullOrWhiteSpace(programmeReference) ? sectionTitle : $"{sectionTitle}, {programmeReference.Trim()}";

    /// <summary>A day's note as the report's bullets: one per line, any bullet mark already typed dropped.</summary>
    public static IReadOnlyList<string> Bullets(string description) =>
        description.Split('\n')
            .Select(line => line.Trim().TrimStart(BulletMarks).Trim())
            .Where(line => line.Length > 0)
            .ToList();

    public static string DecisionRow(ContractorsReportDecision decision) => $"{decision.Title} ({decision.Reference})";

    /// <summary>"Awaiting response", or "Awaiting response, response was due 12 September 2026" once
    /// the due date has passed by the date of issue.</summary>
    public static string DecisionStatus(ContractorsReportDecision decision, DateOnly dateOfIssue)
    {
        if (decision.ResponseDue is not { } due || due >= dateOfIssue) return AwaitingResponse;
        return $"{AwaitingResponse}, response was due {LongDate(due)}";
    }

    /// <summary>"Friday, Monday, Wednesday" — the days ticked; a count when only a count was entered.</summary>
    public static string DaysOnSite(ContractorsReportSubcontractor subcontractor)
    {
        if (subcontractor.DaysOnSite.Count > 0)
            return string.Join(", ", subcontractor.DaysOnSite.Order().Select(day => day.ToString("dddd", Uk)));
        var count = subcontractor.AttendanceDays ?? 0;
        return count == 1 ? "1 day" : $"{count} days";
    }

    /// <summary>"Friday 18 September 2026 (12 photographs)".</summary>
    public static string PhotoDayHeading(ContractorsReportDay day)
    {
        var count = day.Photos.Count;
        var noun = count == 1 ? "photograph" : "photographs";
        return $"{day.Heading} ({count} {noun})";
    }

    /// <summary>"Jewel Bespoke Build Ltd · Contractor's Report No. 31" — the page footer before its page number.</summary>
    public static string FooterLead(ContractorsReportHeader header) => $"Jewel Bespoke Build Ltd · {header.DocumentTitle}";

    public static string LongDate(DateOnly date) => date.ToString(LongDateFormat, Uk);
}
