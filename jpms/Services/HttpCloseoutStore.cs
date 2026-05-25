using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCloseoutStore : ICloseoutStore
{
    private readonly HttpClient httpClient;
    private readonly Dictionary<string, IReadOnlyList<Defect>> defectsByProject = new();
    private readonly Dictionary<string, SettlementRecord?> settlementByProject = new();
    private readonly Dictionary<string, VatAnalysis?> vatByProject = new();
    private readonly Dictionary<string, RetentionRelease?> retentionByProject = new();

    public HttpCloseoutStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<Defect> DefectsFor(string projectId)
    {
        if (!defectsByProject.ContainsKey(projectId)) _ = LoadDefectsAsync(projectId);
        return defectsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Defect>();
    }

    public Defect SaveDefect(Defect defect)
    {
        _ = PostAsync("/api/defects", defect, defect.ProjectId, LoadDefectsAsync);
        return defect;
    }

    public SettlementRecord? SettlementFor(string projectId)
    {
        if (!settlementByProject.ContainsKey(projectId)) _ = LoadSettlementAsync(projectId);
        return settlementByProject.TryGetValue(projectId, out var s) ? s : null;
    }

    public SettlementRecord SaveSettlement(SettlementRecord settlement)
    {
        _ = PostScalarAsync("/api/settlements", settlement, settlement.ProjectId, settlementByProject, LoadSettlementAsync);
        return settlement;
    }

    public VatAnalysis? VatFor(string projectId)
    {
        if (!vatByProject.ContainsKey(projectId)) _ = LoadVatAsync(projectId);
        return vatByProject.TryGetValue(projectId, out var v) ? v : null;
    }

    public VatAnalysis SaveVat(VatAnalysis analysis)
    {
        _ = PostScalarAsync("/api/vat-analyses", analysis, analysis.ProjectId, vatByProject, LoadVatAsync);
        return analysis;
    }

    public RetentionRelease? RetentionFor(string projectId) =>
        retentionByProject.TryGetValue(projectId, out var r) ? r : null;

    public RetentionRelease SaveRetention(RetentionRelease release)
    {
        _ = PostScalarAsync("/api/retention-releases", release, release.ProjectId, retentionByProject, _ => Task.CompletedTask);
        return release;
    }

    private async Task LoadDefectsAsync(string projectId)
    {
        try { defectsByProject[projectId] = (await httpClient.GetFromJsonAsync<List<Defect>>($"/api/projects/{projectId}/defects"))?.AsReadOnly() ?? (IReadOnlyList<Defect>)Array.Empty<Defect>(); OnChange?.Invoke(); }
        catch { defectsByProject[projectId] = Array.Empty<Defect>(); }
    }

    private async Task LoadSettlementAsync(string projectId)
    {
        try { settlementByProject[projectId] = await httpClient.GetFromJsonAsync<SettlementRecord?>($"/api/projects/{projectId}/settlement"); OnChange?.Invoke(); }
        catch { settlementByProject[projectId] = null; }
    }

    private async Task LoadVatAsync(string projectId)
    {
        try { vatByProject[projectId] = await httpClient.GetFromJsonAsync<VatAnalysis?>($"/api/projects/{projectId}/vat"); OnChange?.Invoke(); }
        catch { vatByProject[projectId] = null; }
    }

    private async Task PostAsync<T>(string url, T body, string projectId, Func<string, Task> refresh)
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        await refresh(projectId);
    }

    private async Task PostScalarAsync<T>(string url, T body, string projectId, Dictionary<string, T?> cache, Func<string, Task> refresh) where T : class
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        cache.Remove(projectId);
        await refresh(projectId);
    }
}
