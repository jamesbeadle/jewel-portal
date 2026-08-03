using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// The project picker's "Show completed" toggle (per browser, per user — same pattern as
/// <see cref="AllocationTabStorage"/>). Off by default, so the switcher, the project header's
/// prev/next cycle and project-scoped navigation stay about work in progress; on, completed
/// projects join those loops in their own band at the end of the canonical work order — added
/// so completed projects' records (e.g. the valuation report) stay reachable after handover
/// (decision 2026-08-03). The finance overview deliberately ignores this filter: it is a
/// work-in-progress view whatever the picker shows.
/// </summary>
public sealed class ProjectStageFilter
{
    private const string StorageKeyPrefix = "jpms.showCompletedProjects";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;
    private readonly AuthService auth;
    private Task? loadTask;

    public ProjectStageFilter(IJSRuntime js, AuthService auth)
    {
        this.js = js;
        this.auth = auth;
    }

    public event Action? OnChange;

    public bool IncludeCompleted { get; private set; }

    // One shared load task, mirroring CurrentProjectService: the side-nav and any project page
    // shell asking at boot wait on the same storage read.
    public Task EnsureLoadedAsync() => loadTask ??= LoadAsync();

    private async Task LoadAsync()
    {
        try { IncludeCompleted = await js.InvokeAsync<string?>(GetItem, StorageKey) == "1"; }
        catch { IncludeCompleted = false; }
        OnChange?.Invoke();
    }

    public async Task SetAsync(bool includeCompleted)
    {
        if (IncludeCompleted == includeCompleted) return;
        IncludeCompleted = includeCompleted;
        OnChange?.Invoke();
        try { await js.InvokeVoidAsync(SetItem, StorageKey, includeCompleted ? "1" : "0"); }
        catch { }
    }

    private string StorageKey => $"{StorageKeyPrefix}.{UserKey}";

    private string UserKey => auth.CurrentUser?.Email.Trim().ToLowerInvariant() ?? "anonymous";
}
