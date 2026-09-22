namespace Jewel.JPMS.Models;

/// <summary>
/// The class penalties of the officer's key, as her sheet of 15 Sep 2026 applies them: a fixed
/// deduction for each class PRESENT anywhere on the report — once per class, never once per
/// finding — and one more if any item is a repeat. Two D findings cost one point, not two.
/// </summary>
public static class HsAuditClassPenalties
{
    public const decimal Repeat = 0.05m;
    private const int PointsPerItem = 10;

    public static decimal For(HsAuditClass hsAuditClass) => hsAuditClass switch
    {
        HsAuditClass.A => 0.25m,
        HsAuditClass.B => 0.15m,
        HsAuditClass.C => 0.05m,
        HsAuditClass.D => 0.01m,
        _ => 0m
    };

    public static decimal TotalFor(IReadOnlyCollection<HsAuditItem> items)
    {
        var classesPresent = items.Where(item => item.Class is not null).Select(item => item.Class!.Value).Distinct();
        var classPenalty = classesPresent.Sum(For);
        var hasRepeat = items.Any(IsRepeat);
        return hasRepeat ? classPenalty + Repeat : classPenalty;
    }

    /// <summary>What the sheet prints in an item's Minus column: its class penalty in points,
    /// plus the repeat's — information beside the row, not part of the score.</summary>
    public static decimal PointsOff(HsAuditItem item)
    {
        var classPoints = item.Class is { } hsAuditClass ? For(hsAuditClass) * PointsPerItem : 0m;
        var repeatPoints = IsRepeat(item) ? Repeat * PointsPerItem : 0m;
        return classPoints + repeatPoints;
    }

    public static string PercentText(decimal penalty) => $"−{penalty * 100:0.#}%";

    private static bool IsRepeat(HsAuditItem item) => item.Comment == HsAuditComment.Repeat;
}
