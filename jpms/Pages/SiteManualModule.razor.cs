
namespace Jewel.JPMS.Pages;

/// <summary>A module's own page, re-read after every move so the facts, the text and the history stay true.</summary>
public partial class SiteManualModule
{
    [Inject] private SessionService Session { get; set; } = default!;
    [Inject] private IQueryClient Queries { get; set; } = default!;

    [Parameter] public string ManualModuleId { get; set; } = "";

    private ManualModuleDetail? detail;
    private bool isLoading = true;
    private bool isEditing;

    private bool CanManage => Session.ActiveRole is { } role && ManualRoles.AllowedToManage.Includes(role);

    private bool CanApprove => Session.ActiveRole is { } role && ManualRoles.AllowedToApprove.Includes(role);

    private ManualModule? Module => detail?.Module;

    private string WorkingTextTitle => Module is { Status: ManualModuleStatus.Approved } approved
        ? $"Approved text — v{approved.PublishedVersion}"
        : $"Working text — v{Module?.Version}";

    protected override async Task OnParametersSetAsync()
    {
        await Session.EnsureLoadedAsync();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        isLoading = true;
        try { detail = await Queries.AskAsync(new GetManualModule(ManualModuleId), CancellationToken.None); }
        finally { isLoading = false; }
    }

    private async Task SavedAsync(ManualModule saved)
    {
        isEditing = false;
        await ReloadAsync();
    }
}
