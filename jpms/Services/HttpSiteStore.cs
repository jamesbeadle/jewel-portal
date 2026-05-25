using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpSiteStore : ISiteStore
{
    private readonly HttpClient httpClient;
    private readonly Dictionary<string, IReadOnlyList<SiteReport>> reportsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<ProgrammeTask>> tasksByProject = new();

    public HttpSiteStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<SiteReport> ReportsFor(string projectId)
    {
        if (!reportsByProject.ContainsKey(projectId)) _ = LoadReportsAsync(projectId);
        return reportsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<SiteReport>();
    }

    public IReadOnlyList<ProgrammeTask> ProgrammeFor(string projectId)
    {
        if (!tasksByProject.ContainsKey(projectId)) _ = LoadTasksAsync(projectId);
        return tasksByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ProgrammeTask>();
    }

    public SiteReport SaveReport(SiteReport report)
    {
        _ = PostReportAsync(report);
        return report;
    }

    public ProgrammeTask SaveProgrammeTask(ProgrammeTask task)
    {
        _ = PostTaskAsync(task);
        return task;
    }

    private async Task LoadReportsAsync(string projectId)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<SiteReport>>($"/api/projects/{projectId}/site-reports");
            reportsByProject[projectId] = response?.AsReadOnly() ?? (IReadOnlyList<SiteReport>)Array.Empty<SiteReport>();
            OnChange?.Invoke();
        }
        catch { reportsByProject[projectId] = Array.Empty<SiteReport>(); }
    }

    private async Task LoadTasksAsync(string projectId)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<ProgrammeTask>>($"/api/projects/{projectId}/programme");
            tasksByProject[projectId] = response?.AsReadOnly() ?? (IReadOnlyList<ProgrammeTask>)Array.Empty<ProgrammeTask>();
            OnChange?.Invoke();
        }
        catch { tasksByProject[projectId] = Array.Empty<ProgrammeTask>(); }
    }

    private async Task PostReportAsync(SiteReport report)
    {
        try { await httpClient.PostAsJsonAsync("/api/site-reports", report); } catch { return; }
        reportsByProject.Remove(report.ProjectId);
        await LoadReportsAsync(report.ProjectId);
    }

    private async Task PostTaskAsync(ProgrammeTask task)
    {
        try { await httpClient.PostAsJsonAsync("/api/programme-tasks", task); } catch { return; }
        tasksByProject.Remove(task.ProjectId);
        await LoadTasksAsync(task.ProjectId);
    }
}
