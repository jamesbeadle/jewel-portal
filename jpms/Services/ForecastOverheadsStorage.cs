using System.Globalization;

namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers the Cash Forecast's monthly company-overheads figure, and the FD's per-month
/// overrides of it, per browser per user.
/// INTERIM ONLY: the agreed design (docs/Financial-Reports-and-Nav-Refactor-Plan.md §4.7) makes
/// overheads a system setting the FD owns — one figure, one truth, who/when recorded — which
/// needs an entity and an endpoint. Until the phasing rules are signed off and that ships, this
/// keeps the page usable without pretending: the page labels the figure as unsaved-to-system,
/// and two directors' browsers CAN legitimately differ. Replace with the server setting in the
/// confirmed build and delete this class.
/// </summary>
public sealed class ForecastOverheadsStorage
{
    private const string StorageKeyPrefix = "jpms.forecastOverheads";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;

    public ForecastOverheadsStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<decimal?> ReadAsync(string email)
    {
        try
        {
            var stored = await js.InvokeAsync<string?>(GetItem, StorageKeyFor(email));
            return decimal.TryParse(stored, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }
        catch { return null; }
    }

    public async Task WriteAsync(string email, decimal value)
    {
        try
        {
            await js.InvokeVoidAsync(SetItem, StorageKeyFor(email),
                value.ToString(CultureInfo.InvariantCulture));
        }
        catch { }
    }

    // ---- Per-month overrides (FD, 2026-08-17) -------------------------------------------
    // Months the FD has edited away from the default, stored beside it as
    // "yyyy-MM=amount;yyyy-MM=amount" (invariant culture — no JSON, nothing to version).
    // A month absent here follows the default. Same interim status as the default above.

    public async Task<IReadOnlyDictionary<DateTime, decimal>> ReadOverridesAsync(string email)
    {
        var overrides = new Dictionary<DateTime, decimal>();
        try
        {
            var stored = await js.InvokeAsync<string?>(GetItem, OverridesKeyFor(email)) ?? "";
            foreach (var entry in stored.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = entry.Split('=');
                if (parts.Length != 2) continue;
                var isMonth = DateTime.TryParseExact(parts[0], "yyyy-MM", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var month);
                var isAmount = decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture,
                    out var amount);
                if (isMonth && isAmount) overrides[month] = amount;
            }
        }
        catch { }
        return overrides;
    }

    public async Task WriteOverridesAsync(string email, IReadOnlyDictionary<DateTime, decimal> overrides)
    {
        try
        {
            var stored = string.Join(';', overrides.OrderBy(entry => entry.Key).Select(entry =>
                $"{entry.Key.ToString("yyyy-MM", CultureInfo.InvariantCulture)}={entry.Value.ToString(CultureInfo.InvariantCulture)}"));
            await js.InvokeVoidAsync(SetItem, OverridesKeyFor(email), stored);
        }
        catch { }
    }

    private static string StorageKeyFor(string email) =>
        $"{StorageKeyPrefix}.{email.Trim().ToLowerInvariant()}";

    private static string OverridesKeyFor(string email) => $"{StorageKeyFor(email)}.months";
}
