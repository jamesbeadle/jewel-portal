namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    private bool reinstatingOrder; // ReinstateVariationDialog

    private VariationOrder? ReinstateCandidate => reinstatingOrder ? order : null;

    private bool CanReinstate => order?.Status == VariationOrderStatus.Rejected && CanManage;

    private const string ReinstateHint =
        "Takes the rejection back — the variation returns to Issued, or Quoting if it was never issued";

    private List<DropdownMenu.Item> RejectedStatusMenuItems() => new()
    {
        new(Label: VariationOrderStatus.Rejected.DisplayName(), Disabled: true, Selected: true),
        new(Label: "Reinstate…", OnSelect: EventCallback.Factory.Create(this, OpenReinstate),
            Hint: ReinstateHint, Disabled: busy),
    };

    private DropdownMenu.Item ReinstateMenuItem() => new(
        Label: "Reinstate variation…",
        OnSelect: EventCallback.Factory.Create(this, OpenReinstate),
        Hint: ReinstateHint,
        Disabled: busy, Group: 1);

    private void OpenReinstate()
    {
        error = null;
        reinstatingOrder = true;
    }

    private async Task ReinstateOrder()
    {
        if (busy || order is null) return;
        error = null;
        try
        {
            busy = true;
            order = await Variations.ReinstateAsync(VariationOrderId);
            reinstatingOrder = false;
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't reinstate the variation order. Please try again."; }
        finally { busy = false; }
    }
}
