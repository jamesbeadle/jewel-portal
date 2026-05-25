using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpSubcontractorStore : ISubcontractorStore
{
    private readonly HttpClient httpClient;
    private IReadOnlyList<Subcontractor> cached = Array.Empty<Subcontractor>();
    private bool hasLoaded;
    private readonly Dictionary<string, IReadOnlyList<ComplianceDocument>> complianceBySubId = new();

    public HttpSubcontractorStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<Subcontractor> All()
    {
        if (!hasLoaded) _ = LoadAsync();
        return cached;
    }

    public Subcontractor? Find(string subcontractorId) =>
        cached.FirstOrDefault(sub =>
            string.Equals(sub.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase));

    public Subcontractor Upsert(Subcontractor subcontractor)
    {
        _ = PostSubAsync(subcontractor);
        return subcontractor;
    }

    public IReadOnlyList<ComplianceDocument> ComplianceFor(string subcontractorId)
    {
        if (!complianceBySubId.ContainsKey(subcontractorId)) _ = LoadComplianceAsync(subcontractorId);
        return complianceBySubId.TryGetValue(subcontractorId, out var list) ? list : Array.Empty<ComplianceDocument>();
    }

    public void SaveCompliance(ComplianceDocument document) =>
        _ = SaveComplianceAsync(document);

    private async Task LoadAsync()
    {
        hasLoaded = true;
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<Subcontractor>>("/api/subcontractors");
            cached = response?.AsReadOnly() ?? (IReadOnlyList<Subcontractor>)Array.Empty<Subcontractor>();
            OnChange?.Invoke();
        }
        catch { cached = Array.Empty<Subcontractor>(); }
    }

    private async Task LoadComplianceAsync(string subcontractorId)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<ComplianceDocument>>($"/api/subcontractors/{subcontractorId}/compliance");
            complianceBySubId[subcontractorId] = response?.AsReadOnly() ?? (IReadOnlyList<ComplianceDocument>)Array.Empty<ComplianceDocument>();
            OnChange?.Invoke();
        }
        catch { complianceBySubId[subcontractorId] = Array.Empty<ComplianceDocument>(); }
    }

    private async Task PostSubAsync(Subcontractor sub)
    {
        try { await httpClient.PostAsJsonAsync("/api/subcontractors", sub); } catch { return; }
        await LoadAsync();
    }

    private async Task SaveComplianceAsync(ComplianceDocument document)
    {
        try { await httpClient.PostAsJsonAsync("/api/subcontractors/compliance", document); } catch { return; }
        complianceBySubId.Remove(document.SubcontractorId);
        await LoadComplianceAsync(document.SubcontractorId);
    }
}
