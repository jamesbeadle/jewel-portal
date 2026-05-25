using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpChangeRegister : IChangeRegister
{
    private readonly HttpClient httpClient;
    private IReadOnlyList<ChangeRecord> cached = Array.Empty<ChangeRecord>();
    private bool hasLoaded;

    public HttpChangeRegister(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<ChangeRecord> ForProject(string projectId)
    {
        if (!hasLoaded) _ = LoadAsync();
        return cached.Where(r => string.Equals(r.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
    }

    public IReadOnlyList<ChangeRecord> ForProject(string projectId, ChangeKind kind) =>
        ForProject(projectId).Where(r => r.Kind == kind).ToList().AsReadOnly();

    public ChangeRecord? Find(string changeRecordId) =>
        cached.FirstOrDefault(r =>
            string.Equals(r.ChangeRecordId, changeRecordId, StringComparison.OrdinalIgnoreCase));

    public ChangeRecord Upsert(ChangeRecord record)
    {
        _ = PostAsync(record);
        return record;
    }

    private async Task LoadAsync()
    {
        hasLoaded = true;
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<ChangeRecord>>("/api/changes");
            cached = response?.AsReadOnly() ?? (IReadOnlyList<ChangeRecord>)Array.Empty<ChangeRecord>();
            OnChange?.Invoke();
        }
        catch { cached = Array.Empty<ChangeRecord>(); }
    }

    private async Task PostAsync(ChangeRecord record)
    {
        try { await httpClient.PostAsJsonAsync("/api/changes", record); } catch { return; }
        await LoadAsync();
    }
}
