using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Features.Progress.SitePhotos;

namespace Jewel.JPMS.Pages;

public partial class SitePhotos
{
    private const int MaxPhotosPerDrop = 50;

    private IReadOnlyList<SitePhoto>? photos;
    private bool dataFailed;
    private bool isUploading;
    private string? changing;
    private string? error;
    private string filter = SitePhotoFilter.Unfiled;
    private string scope = SitePhotoScope.WholePool;
    private SitePhotoUploadResult? lastUpload;
    private SitePhoto? viewing;

    // The Progress page's own gates: every internal role reads, the site and project team drop.
    private bool CanRead => Auth.CurrentRoles.Any(role => NavigationRoles.AllInternalRoles.Contains(role));

    private bool CanContribute =>
        Auth.CurrentRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager);

    private string? ScopedProjectId => SitePhotoScope.ProjectIdOf(scope);
    private Project? ScopedProject => ScopedProjectId is { } scopedProjectId ? Projects.Find(scopedProjectId) : null;

    private IReadOnlyList<SitePhoto> Filtered => SitePhotoFilter.Apply(photos ?? Array.Empty<SitePhoto>(), filter, ScopedProjectId);

    private IReadOnlyList<TabItem> Chips => SitePhotoFilter.Chips(photos, ScopedProjectId);

    private IReadOnlyList<TabItem> ScopeChips => SitePhotoScope.Chips(MenuProject);

    private Project? MenuProject => CurrentProject.ResolveFor(Projects.Current) is { } projectId ? Projects.Find(projectId) : null;

    private string Summary => SitePhotoText.Summary(photos ?? Array.Empty<SitePhoto>(), ScopedProject);

    private string EmptyMessage => SitePhotoFilter.EmptyMessage(filter, CanContribute, ScopedProject?.Reference);

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        await Task.WhenAll(LoadAsync(), OpenOnMenuProjectAsync());
    }

    private async Task OpenOnMenuProjectAsync()
    {
        await CurrentProject.EnsureLoadedAsync();
        try { await Projects.RefreshAsync(CancellationToken.None); }
        catch { }
        scope = MenuProject?.ProjectId ?? SitePhotoScope.WholePool;
    }

    private async Task LoadAsync()
    {
        try
        {
            photos = await Store.ListAsync(CancellationToken.None);
            dataFailed = false;
        }
        catch { dataFailed = true; }
    }

    private async Task UploadAsync(InputFileChangeEventArgs e)
    {
        var files = e.GetMultipleFiles(MaxPhotosPerDrop);
        if (files.Count == 0) return;
        isUploading = true;
        error = null;
        try
        {
            lastUpload = await Store.UploadAsync(files, CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception ex) { error = ex.Message; }
        isUploading = false;
    }

    private Task DeleteAsync(string sitePhotoId) =>
        ChangeAsync(sitePhotoId, () => Store.DeleteAsync(sitePhotoId, CancellationToken.None));

    private Task RestoreAsync(string sitePhotoId) =>
        ChangeAsync(sitePhotoId, () => Store.RestoreAsync(sitePhotoId, CancellationToken.None));

    private async Task ChangeAsync(string sitePhotoId, Func<Task> change)
    {
        changing = sitePhotoId;
        error = null;
        try
        {
            await change();
            if (viewing?.SitePhotoId == sitePhotoId) viewing = null;
            await LoadAsync();
        }
        catch (Exception ex) { error = ex.Message; }
        changing = null;
    }
}
