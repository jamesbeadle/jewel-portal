using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpBoqStore : IBoqStore
{
    private readonly HttpClient httpClient;
    private readonly Dictionary<string, IReadOnlyList<BoqLineItem>> linesByProject = new();
    private readonly Dictionary<string, BoqSignOff?> signOffsByProject = new();

    public HttpBoqStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<BoqLineItem> LinesFor(string projectId)
    {
        if (!linesByProject.ContainsKey(projectId)) _ = LoadLinesAsync(projectId);
        return linesByProject.TryGetValue(projectId, out var lines) ? lines : Array.Empty<BoqLineItem>();
    }

    public BoqLineItem Upsert(BoqLineItem line)
    {
        _ = PostAsync("/api/boq", line, line.ProjectId);
        return line;
    }

    public bool Remove(string boqLineItemId)
    {
        _ = httpClient.DeleteAsync($"/api/boq/{boqLineItemId}");
        OnChange?.Invoke();
        return true;
    }

    public decimal TotalFor(string projectId) =>
        LinesFor(projectId).Sum(line => line.LineTotal);

    public BoqSignOff? SignOffFor(string projectId)
    {
        if (!signOffsByProject.ContainsKey(projectId)) _ = LoadSignOffAsync(projectId);
        return signOffsByProject.TryGetValue(projectId, out var s) ? s : null;
    }

    public BoqSignOff RecordSignOff(BoqSignOff signOff)
    {
        _ = PostAsync("/api/boq/sign-offs", signOff, signOff.ProjectId);
        signOffsByProject[signOff.ProjectId] = signOff;
        return signOff;
    }

    private async Task LoadLinesAsync(string projectId)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<BoqLineItem>>($"/api/projects/{projectId}/boq");
            linesByProject[projectId] = response?.AsReadOnly() ?? (IReadOnlyList<BoqLineItem>)Array.Empty<BoqLineItem>();
            OnChange?.Invoke();
        }
        catch { linesByProject[projectId] = Array.Empty<BoqLineItem>(); }
    }

    private async Task LoadSignOffAsync(string projectId)
    {
        try
        {
            signOffsByProject[projectId] = await httpClient.GetFromJsonAsync<BoqSignOff?>($"/api/projects/{projectId}/boq/sign-off");
            OnChange?.Invoke();
        }
        catch { signOffsByProject[projectId] = null; }
    }

    private async Task PostAsync<T>(string url, T body, string projectId)
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        linesByProject.Remove(projectId);
        await LoadLinesAsync(projectId);
    }
}
