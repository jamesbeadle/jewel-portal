using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Features.Sales;

namespace Jewel.JPMS.Pages;

public partial class SalesStrategyDetail
{
    private bool busy;
    private bool generating;
    private string? actionError;

    private bool editOpen;
    private bool addLeadOpen;
    private bool planEditOpen;
    private bool generateOpen;

    private async Task OnEditedAsync(SalesStrategy _)
    {
        editOpen = false;
        await ReloadAsync();
    }

    private async Task OnLeadAddedAsync(Lead lead)
    {
        addLeadOpen = false;
        await ReloadAsync();
        Nav.NavigateTo($"/sales/leads/{lead.LeadId}");
    }

    private async Task SetStatusAsync(SalesStrategyStatus status)
    {
        if (busy || Strategy is not { } strategy || strategy.Status == status) return;
        busy = true; actionError = null;
        try
        {
            await Commands.SendAsync(new SetSalesStrategyStatus(StrategyId, status), CancellationToken.None);
            await ReloadAsync();
            try { await Strategies.RefreshAsync(CancellationToken.None); } catch { }
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private void OpenPlanEdit()
    {
        actionError = null;
        planEditOpen = true;
    }

    private async Task OnPlanSavedAsync()
    {
        planEditOpen = false;
        await ReloadAsync();
    }

    private void OpenGenerate()
    {
        actionError = null;
        generateOpen = true;
    }

    private async Task GenerateAsync(string guidance)
    {
        if (busy) return;
        busy = true; generating = true; actionError = null;
        generateOpen = false;
        try
        {
            await Commands.SendAsync(new GenerateStrategyApproachPlan(StrategyId,
                string.IsNullOrWhiteSpace(guidance) ? null : guidance.Trim()), CancellationToken.None);
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; generating = false; }
    }
}
