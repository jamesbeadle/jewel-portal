using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryRateLibrary : IRateLibrary
{
    private readonly List<Rate> rates = new()
    {
        new("RT-001", "Groundworks", "Strip foundations", "m³",  185m,  "Surrey Concrete Ltd",   DateTimeOffset.UtcNow.AddDays(-12)),
        new("RT-002", "Groundworks", "Reinforced slab",   "m²",   95m,  "Surrey Concrete Ltd",   DateTimeOffset.UtcNow.AddDays(-12)),
        new("RT-003", "Masonry",     "Facing brickwork",  "m²",  142m,  "Hampton Brick Supply",  DateTimeOffset.UtcNow.AddDays(-30)),
        new("RT-004", "Roofing",     "Natural slate roof","m²",  220m,  "Welsh Slate Direct",    DateTimeOffset.UtcNow.AddDays(-95)),
        new("RT-005", "Electrical",  "1st fix sockets",   "nr",   58m,  "Cobham Electrical",     DateTimeOffset.UtcNow.AddDays(-45)),
        new("RT-006", "Joinery",     "Oak staircase",     "nr", 8400m,  "Hartfield Joinery",     DateTimeOffset.UtcNow.AddDays(-7))
    };

    public event Action? OnChange;

    public IReadOnlyList<Rate> All() => rates.AsReadOnly();

    public Rate? Find(string rateId) =>
        rates.FirstOrDefault(rate =>
            string.Equals(rate.RateId, rateId, StringComparison.OrdinalIgnoreCase));

    public Rate Upsert(Rate rate)
    {
        var existing = Find(rate.RateId);
        if (existing is not null) rates.Remove(existing);
        rates.Add(rate);
        OnChange?.Invoke();
        return rate;
    }

    public IReadOnlyList<Rate> Stale(int dayThreshold) =>
        rates.Where(rate => rate.IsStale(dayThreshold))
             .OrderBy(rate => rate.LastPricedAt)
             .ToList()
             .AsReadOnly();
}
