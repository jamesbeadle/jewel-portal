using System.Text.Json;
using Jewel.JPMS.Models;
using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers the projects the user last looked at (per browser, per user — same pattern as
/// <see cref="AllocationTabStorage"/>), most recent first, trimmed by
/// <see cref="RecentProjects.WithVisit"/>. The head of the list is the project that
/// project-scoped side-nav entries open on; the entries behind it feed the picker's Recent
/// group, so the two or three jobs someone is actually working across sit at the top of the
/// dropdown (decision 2026-07-29). Every project page records itself here via ProjectPageShell;
/// falls back to the first active project in the canonical work order when nothing is stored or
/// the stored project has completed.
/// </summary>
public sealed class CurrentProjectService
{
    private const string StorageKeyPrefix = "jpms.recentProjects";
    // The pre-recents key held a single project id; read once as a seed so the remembered
    // project survives the upgrade, never written again.
    private const string LegacyStorageKeyPrefix = "jpms.currentProject";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;
    private readonly AuthService auth;
    private Task? loadTask;
    private List<string> recents = new();

    public CurrentProjectService(IJSRuntime js, AuthService auth)
    {
        this.js = js;
        this.auth = auth;
    }

    public event Action? OnChange;

    /// <summary>The raw last-visited id — prefer <see cref="ResolveFor"/> which validates it.</summary>
    public string? CurrentProjectId => recents.Count > 0 ? recents[0] : null;

    /// <summary>Most recently opened first; the current project is the head entry.</summary>
    public IReadOnlyList<string> RecentProjectIds => recents;

    // A single shared load task, so a project page recording its visit and the side-nav asking
    // for the list at boot both wait on the one storage read — a visit landing mid-read must
    // fold into the stored history, not race it and overwrite the list with a single entry.
    public Task EnsureLoadedAsync() => loadTask ??= LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            recents = Parse(await js.InvokeAsync<string?>(GetItem, StorageKey));
            if (recents.Count == 0)
            {
                var legacy = await js.InvokeAsync<string?>(GetItem, LegacyStorageKey);
                if (!string.IsNullOrWhiteSpace(legacy)) recents = new List<string> { legacy };
            }
        }
        catch { recents = new(); }
        OnChange?.Invoke();
    }

    public async Task RememberAsync(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId)) return;
        await EnsureLoadedAsync(); // The stored history first; the fresh visit then leads it.
        if (string.Equals(CurrentProjectId, projectId, StringComparison.OrdinalIgnoreCase)) return;
        recents = RecentProjects.WithVisit(recents, projectId);
        OnChange?.Invoke();
        try { await js.InvokeVoidAsync(SetItem, StorageKey, JsonSerializer.Serialize(recents)); }
        catch { }
    }

    /// <summary>
    /// The project that project-scoped navigation should target: the remembered project while it
    /// is still active, otherwise the first active project in the canonical work order (live sites
    /// before Defects Period before Leads — the same order the side-nav switcher lists, so the
    /// fallback lands on the top entry the user sees), otherwise whatever was remembered (a
    /// completed project beats nowhere), otherwise null (no projects loaded yet).
    /// </summary>
    public string? ResolveFor(IReadOnlyList<Project>? projects)
    {
        var active = projects?
            .Where(project => project.Stage != ProjectStage.Completed)
            .InWorkOrder()
            .ToList();
        if (active is null || active.Count == 0) return CurrentProjectId;
        if (CurrentProjectId is { } current
            && active.Any(project => string.Equals(project.ProjectId, current, StringComparison.OrdinalIgnoreCase)))
        {
            return current;
        }
        return active[0].ProjectId;
    }

    private static List<string> Parse(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return new();
        try
        {
            var ids = JsonSerializer.Deserialize<List<string>>(stored) ?? new();
            return ids.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        }
        catch { return new(); } // An unreadable list is the same as no list.
    }

    private string StorageKey => $"{StorageKeyPrefix}.{UserKey}";
    private string LegacyStorageKey => $"{LegacyStorageKeyPrefix}.{UserKey}";

    private string UserKey => auth.CurrentUser?.Email.Trim().ToLowerInvariant() ?? "anonymous";
}
