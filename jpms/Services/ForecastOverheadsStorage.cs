using System.Globalization;
using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers the Cash Forecast's monthly company-overheads figure (per browser, per user).
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

    private static string StorageKeyFor(string email) =>
        $"{StorageKeyPrefix}.{email.Trim().ToLowerInvariant()}";
}
