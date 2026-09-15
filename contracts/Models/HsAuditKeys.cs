namespace Jewel.JPMS.Models;

/// <summary>The spreadsheet's four keys, as the UI and the connector read them out.</summary>
public static class HsAuditKeys
{
    public static string DisplayName(this HsAuditRate rate) => rate switch
    {
        HsAuditRate.NotInPlace => "Not in place or poor quality",
        HsAuditRate.OneWeekOutOfDate => "In place, good quality, one week out of date",
        HsAuditRate.UpToDate => "In place, good quality, up to date",
        _ => rate.ToString()
    };

    public static string Letter(this HsAuditClass hsAuditClass) => hsAuditClass.ToString();

    public static string DisplayName(this HsAuditClass hsAuditClass) => hsAuditClass switch
    {
        HsAuditClass.A => "Major hazard — potential prosecution",
        HsAuditClass.B => "Major hazard — potential prohibition notice",
        HsAuditClass.C => "Intermediate hazard — potential improvement notice",
        HsAuditClass.D => "Minor hazard or non-compliance",
        HsAuditClass.E => "Full compliance",
        _ => hsAuditClass.ToString()
    };

    public static string Code(this HsAuditTimeScale timeScale) => timeScale switch
    {
        HsAuditTimeScale.Immediately => "I",
        HsAuditTimeScale.WithinOneDay => "1",
        HsAuditTimeScale.WithinThreeDays => "3",
        HsAuditTimeScale.WithinSevenDays => "7",
        HsAuditTimeScale.WithinOneMonth => "1M",
        HsAuditTimeScale.Ongoing => "O",
        _ => timeScale.ToString()
    };

    public static string DisplayName(this HsAuditTimeScale timeScale) => timeScale switch
    {
        HsAuditTimeScale.Immediately => "Immediately",
        HsAuditTimeScale.WithinOneDay => "Within 24 hours",
        HsAuditTimeScale.WithinThreeDays => "Within 3 days",
        HsAuditTimeScale.WithinSevenDays => "Within 7 days",
        HsAuditTimeScale.WithinOneMonth => "Within a month",
        HsAuditTimeScale.Ongoing => "Ongoing",
        _ => timeScale.ToString()
    };

    public static string Code(this HsAuditComment comment) => comment switch
    {
        HsAuditComment.NotApplicable => "N/A",
        HsAuditComment.Note => "N",
        HsAuditComment.NotChecked => "N/C",
        HsAuditComment.NotSeen => "N/S",
        HsAuditComment.Repeat => "R",
        _ => comment.ToString()
    };

    public static string DisplayName(this HsAuditComment comment) => comment switch
    {
        HsAuditComment.NotApplicable => "Not applicable",
        HsAuditComment.Note => "Note",
        HsAuditComment.NotChecked => "Not checked",
        HsAuditComment.NotSeen => "Not seen / available",
        HsAuditComment.Repeat => "Repeat finding",
        _ => comment.ToString()
    };
}
