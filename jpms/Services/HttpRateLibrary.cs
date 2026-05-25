using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpRateLibrary : IRateLibrary
{
    private readonly HttpClient httpClient;
    private IReadOnlyList<Rate> cached = Array.Empty<Rate>();
    private bool hasLoaded;

    public HttpRateLibrary(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<Rate> All()
    {
        if (!hasLoaded) _ = LoadAsync();
        return cached;
    }

    public Rate? Find(string rateId) =>
        cached.FirstOrDefault(rate =>
            string.Equals(rate.RateId, rateId, StringComparison.OrdinalIgnoreCase));

    public Rate Upsert(Rate rate)
    {
        _ = PostAsync(rate);
        return rate;
    }

    public IReadOnlyList<Rate> Stale(int dayThreshold) =>
        cached.Where(rate => rate.IsStale(dayThreshold))
              .OrderBy(rate => rate.LastPricedAt)
              .ToList()
              .AsReadOnly();

    private async Task LoadAsync()
    {
        hasLoaded = true;
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<Rate>>("/api/rates");
            cached = response?.AsReadOnly() ?? (IReadOnlyList<Rate>)Array.Empty<Rate>();
            OnChange?.Invoke();
        }
        catch { cached = Array.Empty<Rate>(); }
    }

    private async Task PostAsync(Rate rate)
    {
        try { await httpClient.PostAsJsonAsync("/api/rates", rate); } catch { return; }
        await LoadAsync();
    }
}
