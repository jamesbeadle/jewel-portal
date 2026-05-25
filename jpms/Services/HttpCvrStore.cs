using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCvrStore : ICvrStore
{
    private readonly HttpClient httpClient;
    private readonly Dictionary<string, IReadOnlyList<CvrSnapshot>> snapshotsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<ForecastComponent>> forecastByProject = new();
    private readonly Dictionary<string, IReadOnlyList<QsAccrual>> accrualsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<PrelimItem>> prelimsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<PrelimForecastEntry>> entriesByPrelim = new();
    private readonly Dictionary<string, IReadOnlyList<Eot>> eotsByProject = new();

    public HttpCvrStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<CvrSnapshot> SnapshotsFor(string projectId) =>
        Cached(snapshotsByProject, projectId, $"/api/projects/{projectId}/cvr-snapshots");

    public CvrSnapshot? LatestSnapshot(string projectId) => SnapshotsFor(projectId).FirstOrDefault();

    public IReadOnlyList<CvrPackageRow> PackagesFor(string projectId) => Array.Empty<CvrPackageRow>();

    public IReadOnlyList<ForecastComponent> ForecastComponentsFor(string projectId) =>
        Cached(forecastByProject, projectId, $"/api/projects/{projectId}/forecast-components");

    public IReadOnlyList<QsAccrual> AccrualsFor(string projectId) =>
        Cached(accrualsByProject, projectId, $"/api/projects/{projectId}/qs-accruals");

    public QsAccrual SaveAccrual(QsAccrual accrual)
    {
        _ = PostAndRefreshAsync("/api/qs-accruals", accrual, accrual.ProjectId, accrualsByProject, $"/api/projects/{accrual.ProjectId}/qs-accruals");
        return accrual;
    }

    public IReadOnlyList<PrelimItem> PrelimsFor(string projectId) =>
        Cached(prelimsByProject, projectId, $"/api/projects/{projectId}/prelims");

    public IReadOnlyList<PrelimForecastEntry> PrelimEntriesFor(string prelimItemId) =>
        Cached(entriesByPrelim, prelimItemId, $"/api/prelims/{prelimItemId}/entries");

    public IReadOnlyList<Eot> EotsFor(string projectId) =>
        Cached(eotsByProject, projectId, $"/api/projects/{projectId}/eots");

    public Eot SaveEot(Eot eot)
    {
        _ = PostAndRefreshAsync("/api/eots", eot, eot.ProjectId, eotsByProject, $"/api/projects/{eot.ProjectId}/eots");
        return eot;
    }

    private IReadOnlyList<T> Cached<T>(Dictionary<string, IReadOnlyList<T>> store, string key, string url)
    {
        if (!store.ContainsKey(key)) _ = LoadAsync(store, key, url);
        return store.TryGetValue(key, out var list) ? list : Array.Empty<T>();
    }

    private async Task LoadAsync<T>(Dictionary<string, IReadOnlyList<T>> store, string key, string url)
    {
        try { store[key] = (await httpClient.GetFromJsonAsync<List<T>>(url))?.AsReadOnly() ?? (IReadOnlyList<T>)Array.Empty<T>(); OnChange?.Invoke(); }
        catch { store[key] = Array.Empty<T>(); }
    }

    private async Task PostAndRefreshAsync<T>(string url, T body, string key, Dictionary<string, IReadOnlyList<T>> store, string refreshUrl)
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        store.Remove(key);
        await LoadAsync(store, key, refreshUrl);
    }
}
