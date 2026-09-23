using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Features.Procurement.Acceptance;

namespace Jewel.JPMS.Pages;

public partial class WorkOrderAcceptance
{
    private const string CouldNotRecord = "Couldn't record your acceptance. Please try again.";

    [Parameter] public string Token { get; set; } = "";

    private bool isLoading = true;
    private bool isAccepting;
    private string name = "";
    private string? error;
    private string? note;
    private WorkOrderAcceptanceView? view;

    private string Title => view is null ? "Work order" : view.Order.Reference;

    private bool IsLinkInvalid => !isLoading && view is null;

    protected override async Task OnInitializedAsync()
    {
        view = await WorkOrderAcceptanceRequests.ViewAsync(Http, Token);
        name = view?.SupplierContactName ?? "";
        isLoading = false;
    }

    private async Task AcceptAsync()
    {
        if (isAccepting || view is null) return;
        error = null;
        note = null;
        isAccepting = true;
        try
        {
            var outcome = await WorkOrderAcceptanceRequests.AcceptAsync(Http, Token, new WorkOrderAcceptanceSignature(name));
            if (outcome.View is null) { error = outcome.Error ?? CouldNotRecord; return; }
            view = outcome.View;
            note = $"{view.Order.Reference} accepted — thank you. Your acceptance is recorded on the purchase order.";
        }
        finally { isAccepting = false; }
    }

    private async Task PrintAsync() => await JS.InvokeVoidAsync("window.print");
}
