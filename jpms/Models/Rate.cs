namespace Jewel.JPMS.Models;

public sealed record Rate(
    string RateId,
    string Trade,
    string Description,
    string Unit,
    decimal Value,
    string SupplierName,
    DateTimeOffset LastPricedAt);

public static class RateExtensions
{
    public static bool IsStale(this Rate rate, int dayThreshold) =>
        (DateTimeOffset.UtcNow - rate.LastPricedAt).TotalDays > dayThreshold;
}
