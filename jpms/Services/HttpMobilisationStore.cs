using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpMobilisationStore : IMobilisationStore
{
    private readonly HttpClient httpClient;
    private readonly Dictionary<string, IReadOnlyList<MobilisationItem>> itemsByProject = new();

    public HttpMobilisationStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public MobilisationChecklist For(string projectId)
    {
        if (!itemsByProject.ContainsKey(projectId)) _ = LoadAsync(projectId);
        var items = itemsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<MobilisationItem>();
        return new MobilisationChecklist(projectId, items);
    }

    public void ToggleItem(string mobilisationItemId)
    {
        var current = itemsByProject.Values.SelectMany(list => list)
            .FirstOrDefault(item => item.MobilisationItemId == mobilisationItemId);
        if (current is null) return;
        var nextComplete = !current.IsComplete;
        var updated = current with
        {
            IsComplete = nextComplete,
            CompletedAt = nextComplete ? DateTimeOffset.UtcNow : null
        };
        _ = PostAsync(updated);
    }

    private async Task LoadAsync(string projectId)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<MobilisationItem>>($"/api/projects/{projectId}/mobilisation");
            itemsByProject[projectId] = response?.AsReadOnly() ?? (IReadOnlyList<MobilisationItem>)Array.Empty<MobilisationItem>();
            OnChange?.Invoke();
        }
        catch { itemsByProject[projectId] = Array.Empty<MobilisationItem>(); }
    }

    private async Task PostAsync(MobilisationItem item)
    {
        try { await httpClient.PostAsJsonAsync("/api/mobilisation-items", item); } catch { return; }
        itemsByProject.Remove(item.ProjectId);
        await LoadAsync(item.ProjectId);
    }
}
