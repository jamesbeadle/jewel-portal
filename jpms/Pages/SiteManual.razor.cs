using Jewel.JPMS.Features.Manual;

namespace Jewel.JPMS.Pages;

/// <summary>
/// The office master of the site manual: every module in family order. The office adds a module or,
/// while the manual is empty, loads the JBB baseline as drafts; everyone on staff reads the rows.
/// </summary>
public partial class SiteManual
{
    [Inject] private SessionService Session { get; set; } = default!;
    [Inject] private ManualModulesReadModel Modules { get; set; } = default!;
    [Inject] private ICommandSender Commands { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool isCreating;
    private bool isImporting;
    private string? note;

    private bool CanManage => Session.ActiveRole is { } role && ManualRoles.AllowedToManage.Includes(role);

    private bool IsEmpty => Modules.Current is { Count: 0 };

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        Modules.OnChanged += StateHasChanged;
        await RefreshAsync();
    }

    public void Dispose() => Modules.OnChanged -= StateHasChanged;

    private async Task RefreshAsync()
    {
        try { await Modules.RefreshAsync(CancellationToken.None); }
        catch (Exception) { note = "Couldn't load the manual — reload to try again."; }
    }

    private Task CreatedAsync(ManualModule created)
    {
        isCreating = false;
        Nav.NavigateTo(ManualViews.ModuleAddress(created.ManualModuleId));
        return Task.CompletedTask;
    }

    private async Task ImportBaselineAsync()
    {
        isImporting = true;
        try
        {
            var imported = await Commands.SendAsync(new ImportManualBaseline(), CancellationToken.None);
            note = $"Loaded {imported.CreatedCount} modules from the JBB Site Manager Manual v0.13 as drafts. Name each one's owner and approver, then send it for review.";
            await RefreshAsync();
        }
        catch (CommandFailedException refusal) { note = refusal.Message; }
        finally { isImporting = false; }
    }
}
