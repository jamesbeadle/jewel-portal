using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Features.Sales;

namespace Jewel.JPMS.Pages;

public partial class SalesEstimateDetail
{
    private bool busy;
    private bool dirty;
    private string? actionError;
    private string? actionNote;

    private bool editOpen;
    private LeadEstimate? moving;

    private List<DropdownMenu.Item> ActionMenuItems
    {
        get
        {
            var items = new List<DropdownMenu.Item>();
            if (Estimate is not { } estimate) return items;
            items.Add(new(Label: "Download PDF", Href: $"/api/sales/estimates/{estimate.EstimateId}/document",
                Hint: "The estimate document — cover, summary, breakdown chart, itemised sections, contact page — rendered from the register on every download"));
            if (CanWork && estimate.Status.IsOpen())
            {
                items.Add(new(Label: "Edit details…", OnSelect: EventCallback.Factory.Create(this, () => { editOpen = true; }),
                    Hint: "Scope, architect, price due, budget mentioned, notes", Group: 1));
                items.Add(new(Label: "Move status…", OnSelect: EventCallback.Factory.Create(this, () => { moving = estimate; }),
                    Hint: "Received → Pricing → Submitted → Won / Lost", Group: 1));
            }
            return items;
        }
    }

    private async Task SaveBreakdownAsync()
    {
        if (busy || Estimate is null) return;
        busy = true; actionError = null; actionNote = null;
        try
        {
            var payload = breakdown.ToPayload();
            var saved = await Commands.SendAsync(new SetEstimateBreakdown(EstimateId, payload), CancellationToken.None);
            actionNote = $"{saved.Reference} breakdown saved — {payload.Count} section{(payload.Count == 1 ? "" : "s")}, total {WholeMoney(saved.Total ?? 0)} ex VAT.";
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task SaveNarrativeAsync(EstimateNarrative narrative)
    {
        if (busy || Estimate is not { } estimate) return;
        busy = true; actionError = null; actionNote = null;
        try
        {
            await Commands.SendAsync(new UpdateEstimateDetails(estimate.EstimateId, estimate.Scope, estimate.ArchitectName,
                estimate.PriceDueOn, estimate.BudgetMentioned, estimate.Total, estimate.Notes,
                narrative.ExecutiveSummary.Trim(), narrative.BuildTime.Trim(), narrative.Exclusions.Trim()), CancellationToken.None);
            actionNote = "The document text is saved.";
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task OnEditedAsync(LeadEstimate _) { editOpen = false; await ReloadAsync(); }
    private async Task OnMovedAsync(LeadEstimate _) { moving = null; await ReloadAsync(); }
}
