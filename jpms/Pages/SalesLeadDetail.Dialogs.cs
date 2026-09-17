using Jewel.JPMS.Features.Sales;

namespace Jewel.JPMS.Pages;

public partial class SalesLeadDetail
{
    private string? actionNote;
    private bool editOpen;
    private bool winOpen;
    private bool logOpen;
    private bool deleteOpen;
    private LeadStage? moveTo;

    private async Task OnEditedAsync(Lead _)
    {
        editOpen = false;
        await ReloadAsync();
    }

    private async Task OnMovedAsync()
    {
        moveTo = null;
        await ReloadAsync();
    }

    private async Task OnWonAsync(string projectReference)
    {
        winOpen = false;
        actionNote = $"{Lead?.Reference} is Won — the client account and project {projectReference} are created; open the project from Details.";
        await ReloadAsync();
    }

    private void OnDeleted()
    {
        deleteOpen = false;
        Nav.NavigateTo("/sales/leads");
        Detail.Forget(LeadId);
    }

    private async Task OnLoggedAsync()
    {
        logOpen = false;
        await ReloadAsync();
    }
}
