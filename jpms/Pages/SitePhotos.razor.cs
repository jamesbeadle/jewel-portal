using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Features.Progress.SitePhotos;

namespace Jewel.JPMS.Pages;

public partial class SitePhotos
{
    private const int MaxPhotosPerDrop = 50;

    private IReadOnlyList<SitePhoto>? photos;
    private bool dataFailed;
    private bool isUploading;
    private string? deleting;
    private string? error;
    private string filter = SitePhotoFilter.Unfiled;
    private SitePhotoUploadResult? lastUpload;
    private SitePhoto? viewing;

    // The Progress page's own gates: every internal role reads the pool, the site and project
    // team (and the MD, who helps out) drop into it.
    private bool CanRead => Auth.CurrentRoles.Any(role => NavigationRoles.AllInternalRoles.Contains(role));

    private bool CanContribute =>
        Auth.CurrentRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager);

    private IReadOnlyList<SitePhoto> Filtered => SitePhotoFilter.Apply(photos ?? Array.Empty<SitePhoto>(), filter);

    private IReadOnlyList<TabItem> Chips => SitePhotoFilter.Chips(photos);

    private string Summary => SitePhotoText.Summary(photos ?? Array.Empty<SitePhoto>());

    private string EmptyMessage => SitePhotoFilter.EmptyMessage(filter, CanContribute);

    private static string UploadSummary(SitePhotoUploadResult upload) => SitePhotoText.UploadSummary(upload);

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        await LoadAsync();
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

    private async Task DeleteAsync(string sitePhotoId)
    {
        deleting = sitePhotoId;
        error = null;
        try
        {
            await Store.DeleteAsync(sitePhotoId, CancellationToken.None);
            if (viewing?.SitePhotoId == sitePhotoId) viewing = null;
            await LoadAsync();
        }
        catch (Exception ex) { error = ex.Message; }
        deleting = null;
    }
}
