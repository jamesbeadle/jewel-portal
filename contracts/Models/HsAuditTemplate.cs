namespace Jewel.JPMS.Models;

/// <summary>
/// The H&S inspection framework as the officer runs it — Katy-Louise Hicks's "H&S Inspection
/// Report Framework" workbook, item for item, so a portal audit is the same audit she was filing
/// by spreadsheet. Every audit records the version it was planted from; a change to the framework
/// is a new version, never an edit of a planted audit. Version 2026-08-27 was her 27 Aug workbook
/// (182 items); 2026-09-15 is her simplified one (165 items, HsAuditTemplateItems.Current).
/// </summary>
public static class HsAuditTemplate
{
    public const string Version = "2026-09-15";
    public const string FirstVersion = "2026-08-27";

    public static readonly IReadOnlyList<HsAuditTemplateSection> Sections = new HsAuditTemplateSection[]
    {
        new(1, "Documentation"),
        new(2, "Temporary Works"),
        new(3, "Notices & Posters"),
        new(4, "Specific Training"),
        new(5, "General"),
        new(6, "Environment"),
        new(7, "Work Activities"),
        new(8, "Working at Heights"),
        new(9, "Traffic Management"),
        new(10, "Fire & Emergency"),
        new(11, "Offices and Welfare"),
    };

    public static IReadOnlyList<HsAuditTemplateItem> Items => HsAuditTemplateItems.Current;

    public static string SectionName(int section) =>
        Sections.FirstOrDefault(candidate => candidate.Number == section)?.Name ?? section.ToString();
}

public sealed record HsAuditTemplateSection(int Number, string Name);

public sealed record HsAuditTemplateItem(string Code, int Section, string Name);
