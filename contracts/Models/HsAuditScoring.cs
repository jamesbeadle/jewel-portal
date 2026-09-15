namespace Jewel.JPMS.Models;

/// <summary>
/// The spreadsheet's score, exactly (decision 2026-09-15, Nigel: match the spreadsheet):
/// score = max(0, (Σ rate − Σ minus) ÷ (rated items × 10)). An item with no rate is not in the
/// denominator, so a "not seen" item costs nothing; the class penalties the key describes are
/// NOT applied by the portal — Minus is whatever the officer keyed. Null until an item is rated.
/// </summary>
public static class HsAuditScoring
{
    private const int PointsPerItem = 10;

    public static decimal? ScoreOf(IEnumerable<HsAuditItem> items)
    {
        var rated = items.Where(item => item.Rate is not null).ToList();
        if (rated.Count == 0) return null;
        var earned = rated.Sum(item => (int)item.Rate!.Value) - rated.Sum(item => item.Minus);
        var available = rated.Count * PointsPerItem;
        return Math.Max(0m, Math.Round((decimal)earned / available, 4));
    }

    private const decimal FairFrom = 0.70m;
    private const decimal GoodFrom = 0.85m;
    private const decimal VeryGoodFrom = 0.95m;

    public static HsAuditRating RatingOf(decimal score)
    {
        if (score >= VeryGoodFrom) return HsAuditRating.VeryGood;
        if (score >= GoodFrom) return HsAuditRating.Good;
        if (score >= FairFrom) return HsAuditRating.Fair;
        return HsAuditRating.Poor;
    }

    public static string PercentText(decimal score) => $"{Math.Round(score * 100):0}%";
}
