namespace Jewel.JPMS.Models;

/// <summary>
/// The officer's sheet's score, exactly (her workbook of 15 Sep 2026, Analytics!H11):
/// rate average − the class penalties, floored at zero. The rate average is Σ rate ÷ (rated
/// items × 10) — an item with no rate is not in the denominator, so a "not seen" item costs
/// nothing. The penalties are HsAuditClassPenalties: once per class present, plus the repeat.
/// The hand-keyed Minus of the 27 Aug workbook is no longer in the score. Null until an item is
/// rated.
/// </summary>
public static class HsAuditScoring
{
    private const int PointsPerItem = 10;

    public static decimal? ScoreOf(IEnumerable<HsAuditItem> items)
    {
        var everyItem = items.ToList();
        var rateAverage = RateAverageOf(everyItem);
        if (rateAverage is null) return null;
        var penalty = HsAuditClassPenalties.TotalFor(everyItem);
        return Math.Max(0m, Math.Round(rateAverage.Value - penalty, 4));
    }

    public static decimal? RateAverageOf(IReadOnlyCollection<HsAuditItem> items)
    {
        var rated = items.Where(item => item.Rate is not null).ToList();
        if (rated.Count == 0) return null;
        var earned = rated.Sum(item => (int)item.Rate!.Value);
        var available = rated.Count * PointsPerItem;
        return Math.Round((decimal)earned / available, 4);
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
