using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Documents;

/// <summary>The words on the audit report, in one place: what a blank reads as, the keys the
/// officer's sheet prints, the two declarations, and which rows are worth printing.</summary>
public static class HsAuditReportText
{
    public const string DocumentTitle = "H&S Inspection Report";
    public const string Dash = "—";
    public const string NotYetScored = "Not yet scored";
    public const string NothingRecorded = "Nothing recorded in this section.";
    public const string RateKey = "Rate  0 not in place or poor quality · 5 in place, good quality, a week out of date · 10 in place, good quality, up to date.";
    public const string ClassKey = "Class  A major hazard, potential prosecution (−25%) · B major hazard, potential prohibition notice (−15%) · C intermediate hazard, potential improvement notice (−5%) · D minor hazard or non-compliance (−1%) · E full compliance. Each class present on the report is deducted once; a repeat finding (R) deducts 5% once.";
    public const string TimeScaleKey = "Time-scale  I immediately · 1 within 24 hours · 3 within 3 days · 7 within 7 days · 1M within a month · O ongoing.   Comment  N/A not applicable · N note · N/C not checked · N/S not seen or available · R repeat.";
    public const string OfficerDeclaration = "I confirm that this report is a true reflection of the condition of the site and the works being undertaken during my visit, and that its findings have been explained to the manager or supervisor.";
    public const string ManagerDeclaration = "I confirm that the actions from this report have been completed to a satisfactory standard.";
    public const string NotYetDeclared = "Not yet signed.";

    public static string OrDash(string? text) => string.IsNullOrWhiteSpace(text) ? Dash : text.Trim();

    public static string Score(HsAudit audit) =>
        audit.Score is { } score
            ? $"{HsAuditScoring.PercentText(score)} · {HsAuditScoring.RatingOf(score).DisplayName()}"
            : NotYetScored;

    public static string PreviousScore(HsAudit audit) =>
        audit.PreviousScore is { } previous ? HsAuditScoring.PercentText(previous) : Dash;

    public static string Count(int? count) => count is { } number ? number.ToString() : Dash;

    public static string DeclaredBy(string name, DateTimeOffset? at) =>
        at is { } when ? $"{OrDash(name)}, {JewelDocumentStyle.Date(when)}" : NotYetDeclared;

    /// <summary>A row is printed when the officer wrote anything on it; a blank row on a
    /// 165-item framework is noise, not a finding.</summary>
    public static bool IsWorthPrinting(HsAuditItem item) =>
        item.Rate is not null || item.Comment is not null || !string.IsNullOrWhiteSpace(item.Findings) || !string.IsNullOrWhiteSpace(item.OwnerName);

    public static string Provenance(HsAudit audit, DateTimeOffset generatedAt) =>
        $"{audit.Reference} · {audit.Status.DisplayName()} · generated {JewelDocumentStyle.DateAndTime(generatedAt)}";
}
