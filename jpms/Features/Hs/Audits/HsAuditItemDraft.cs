using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Features.Hs.Audits;

/// <summary>One item's row on the form as the officer is editing it — bound directly by the
/// section panel's controls, then turned into the entry the write takes. IsDirty is what
/// "Save section" sends.</summary>
public sealed class HsAuditItemDraft
{
    private readonly HsAuditItem original;

    public HsAuditItemDraft(HsAuditItem item)
    {
        original = item;
        Comment = item.Comment;
        Rate = item.Rate;
        Class = item.Class;
        Minus = item.Minus;
        TimeScale = item.TimeScale;
        Findings = item.Findings;
        OwnerName = item.OwnerName;
        DateRectified = item.DateRectified;
    }

    public string HsAuditItemId => original.HsAuditItemId;
    public string Code => original.Code;
    public int Section => original.Section;
    public string Name => original.Name;
    public string? HsRecordId => original.HsRecordId;

    public HsAuditComment? Comment { get; set; }
    public HsAuditRate? Rate { get; set; }
    public HsAuditClass? Class { get; set; }
    public int Minus { get; set; }
    public HsAuditTimeScale? TimeScale { get; set; }
    public string Findings { get; set; }
    public string OwnerName { get; set; }
    public DateTimeOffset? DateRectified { get; set; }

    public bool IsDirty =>
        Comment != original.Comment
        || Rate != original.Rate
        || Class != original.Class
        || Minus != original.Minus
        || TimeScale != original.TimeScale
        || Findings.Trim() != original.Findings
        || OwnerName.Trim() != original.OwnerName
        || DateRectified != original.DateRectified;

    /// <summary>The sheet's Minus column as a reading: the points the class and a repeat take
    /// off this item. Information beside the row — the score deducts each class once.</summary>
    public string PointsOffText
    {
        get
        {
            var pointsOff = HsAuditClassPenalties.PointsOff(AsItem());
            return pointsOff == 0m ? "—" : $"−{pointsOff:0.#}";
        }
    }

    public HsAuditItemEntry ToEntry() =>
        new(HsAuditItemId, Comment, Rate, Class, Minus, TimeScale, Findings.Trim(), OwnerName.Trim(), DateRectified);

    /// <summary>The live score across every draft — what the chip shows as the officer types,
    /// before a save; HsAuditScoring is the one rule.</summary>
    public static decimal? LiveScore(IEnumerable<HsAuditItemDraft> drafts) =>
        HsAuditScoring.ScoreOf(drafts.Select(draft => draft.AsItem()));

    private HsAuditItem AsItem() =>
        new(HsAuditItemId, "", Code, Section, Name, Comment, Rate, Class, Minus, TimeScale, Findings, OwnerName, DateRectified, HsRecordId);
}
