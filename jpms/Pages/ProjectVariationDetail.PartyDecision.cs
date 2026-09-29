using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    private bool approvingAsInstructed;

    private bool CanDecide => Session.CanOpen(VariationApprovalRoles.Deciders);

    private bool IsAwaitingTheirDecision => order is { } current && current.Status.IsPreApproval();

    private string? PartyPrimaryLabel => CanDecide && IsAwaitingTheirDecision ? "Approve variation" : null;

    private bool PrimaryDisabled => !CanManage && !HasStagedBuildUp;

    private string PartyApprovalSentence =>
        VariationApproval.FromStagedBuildUp(order!) is { } staged
            ? $"Approves the variation at {MoneyFormats.Money(staged.Total)} as Jewel priced it, and instructs the works."
            : "Jewel has not priced this variation yet — there is nothing to approve until the line items are on it.";

    private List<DropdownMenu.Item> PartyStatusMenuItems()
    {
        if (!CanDecide || !IsAwaitingTheirDecision) return new List<DropdownMenu.Item>();
        return new List<DropdownMenu.Item>
        {
            new(Label: "Reject variation…",
                OnSelect: EventCallback.Factory.Create(this, () => decliningOrder = true),
                Hint: "Declines the variation as priced — Jewel can reinstate it if that changes",
                Disabled: busy)
        };
    }

    private Task OpenPartyApproval()
    {
        approvingAsInstructed = true;
        return Task.CompletedTask;
    }

    private async Task ApproveAsInstructed()
    {
        if (order is null || VariationApproval.FromStagedBuildUp(order) is not { } staged) return;
        await ApproveWithLines(staged);
        approvingAsInstructed = false;
    }
}
