using Jewel.JPMS.Models;
using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers the project the user last looked at (per browser, per user — same pattern as
/// <see cref="AllocationTabStorage"/>) so project-scoped side-nav entries like Financials →
/// Project financials open straight onto the project they work on. Every project page records
/// itself here via ProjectPageShell; falls back to the first active project by reference when
/// nothing is stored or the stored project has completed.
/// </summary>
public sealed class CurrentProjectService
{
    private const string StorageKeyPrefix = "jpms.currentProject";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;
    private readonly AuthService auth;
    private bool loaded;
    private string? currentProjectId;

    public CurrentProjectService(IJSRuntime js, AuthService auth)
    {
        this.js = js;
        this.auth = auth;
    }

    public event Action? OnChange;

    /// <summary>The raw remembered id — prefer <see cref="ResolveFor"/> which validates it.</summary>
    public string? CurrentProjectId => currentProjectId;

    public async Task EnsureLoadedAsync()
    {
        if (loaded) return;
        loaded = true;
        try { currentProjectId = await js.InvokeAsync<string?>(GetItem, StorageKey); }
        catch { currentProjectId = null; }
        OnChange?.Invoke();
    }

    public async Task RememberAsync(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId)) return;
        loaded = true; // A fresh visit outranks whatever storage held.
        if (string.Equals(currentProjectId, projectId, StringComparison.OrdinalIgnoreCase)) return;
        currentProjectId = projectId;
        OnChange?.Invoke();
        try { await js.InvokeVoidAsync(SetItem, StorageKey, projectId); }
        catch { }
    }

    /// <summary>
    /// The project that project-scoped navigation should target: the remembered project while it
    /// is still active, otherwise the first active project by reference, otherwise whatever was
    /// remembered (a completed project beats nowhere), otherwise null (no projects loaded yet).
    /// </summary>
    public string? ResolveFor(IReadOnlyList<Project>? projects)
    {
        var active = projects?
            .Where(project => project.Stage != ProjectStage.Completed)
            .OrderBy(project => project.Reference, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (active is null || active.Count == 0) return currentProjectId;
        if (currentProjectId is not null
            && active.Any(project => string.Equals(project.ProjectId, currentProjectId, StringComparison.OrdinalIgnoreCase)))
        {
            return currentProjectId;
        }
        return active[0].ProjectId;
    }

    private string StorageKey =>
        $"{StorageKeyPrefix}.{auth.CurrentUser?.Email.Trim().ToLowerInvariant() ?? "anonymous"}";
}
