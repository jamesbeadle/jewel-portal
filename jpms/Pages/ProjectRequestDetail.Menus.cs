using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    // ---- Title-bar menus --------------------------------------------------------------------------
    // Both menus are built here rather than inline in markup, so the whole set of things you can do
    // to a request — and exactly what gates each one — reads as a single list in one place.

    private List<DropdownMenu.Item> StatusMenuItems => StatusOptions
        .Select(option => new DropdownMenu.Item(
            Label: option.DisplayName(),
            OnSelect: EventCallback.Factory.Create(this, () => ChangeStatusFromPill(option)),
            Hint: option.Hint(),
            Disabled: busy || option == record?.Status,
            Selected: option == record?.Status))
        .ToList();

    private List<DropdownMenu.Item> ActionMenuItems
    {
        get
        {
            var items = new List<DropdownMenu.Item>();
            if (record is null) return items;

            // Group 0 — the document itself.
            if (record.Kind.IsEmailable())
                items.Add(new(Label: $"Download {record.Kind.DisplayName()} PDF", Href: DocumentHref,
                    Hint: "Regenerated from the register on every download, so it always reflects the request as it stands"));

            // Group 1 — moving the request along.
            if (record.Status is not RequestStatus.Closed)
            {
                items.Add(new(Label: "Record response…", OnSelect: EventCallback.Factory.Create(this, OpenResponseModal),
                    Hint: "The formal answer recorded on the request — it prints on the official document", Group: 1));
                items.Add(new(Label: "Close request…", OnSelect: EventCallback.Factory.Create(this, OpenCloseConfirm), Group: 1));
            }
            else
            {
                items.Add(new(Label: "Reopen request", OnSelect: EventCallback.Factory.Create(this, ReopenFromMenu), Group: 1));
            }

            // Promotion also has the primary slot when there is nothing to email yet; it stays here
            // so the menu is a complete list of what can be done, not a list of leftovers.
            if (CanEditDetails && record.Kind is not RequestType.Rfi)
                items.Add(new(Label: "Promote to RFI", OnSelect: EventCallback.Factory.Create(this, PromoteToRfi),
                    Disabled: busy, Group: 1));

            // Group 2 — editing, gathering up the four inline "Edit" links the panels used to carry.
            if (CanEditDetails)
            {
                items.Add(new(Label: "Edit subject & reference…", OnSelect: EventCallback.Factory.Create(this, OpenHeaderEdit), Group: 2));
                items.Add(new(Label: "Edit dates & references…", OnSelect: EventCallback.Factory.Create(this, OpenFactsEdit),
                    Hint: "Date issued, response due, drawing references", Group: 2));
                items.Add(new(Label: "Edit description…", OnSelect: EventCallback.Factory.Create(this, OpenDetailEdit), Group: 2));
                items.Add(new(Label: "Edit official form", OnSelect: EventCallback.Factory.Create(this, OpenFormEditor),
                    Hint: "The itemised queries and the basis / response / impact sections",
                    Disabled: editingForm, Group: 2));

                if (record.Kind is RequestType.Rfi)
                    items.Add(new(Label: record.CriticalPath ? "Remove critical path tag" : "Mark as critical path",
                        OnSelect: EventCallback.Factory.Create(this, ToggleCriticalPathFromMenu),
                        Hint: "Tagged RFIs show under Critical Path RFIs on the Programme tab", Group: 2));

                // Hidden once a variation exists (including one attached via the register's Link…
                // repair) — that step has already been taken.
                if (record.Kind is RequestType.Rfi && !record.HasRfq && variation is null)
                    items.Add(new(Label: "Create bid packages",
                        OnSelect: EventCallback.Factory.Create(this, CreateBidPackagesFromMenu),
                        Hint: "Marks this RFI as carrying an RFQ, so the variation's scope can go out to tender", Group: 2));

            }

            // The same action as the rail's button, mirrored into the menu so it sits with the
            // other "move this record along" steps. Any RFI can raise one — no RFQ required.
            // Outside the CanEditDetails block on purpose: raising a variation is the wider gate
            // (CanRaiseVariation), and the two must agree or the MD and the QS get a button on the
            // rail and no matching row in the menu.
            if (record.Kind is RequestType.Rfi && variation is null && CanRaiseVariation)
                items.Add(new(Label: "Create Variation Order Quote…",
                    OnSelect: EventCallback.Factory.Create(this, OpenVariationDraft),
                    Hint: "Drafts a priced quote from this RFI and its tagged emails for you to review", Group: 1));

            // Group 3 — the destructive tail, kept last and marked.
            if (CanTriage)
                items.Add(new(Label: "Return to Control Centre…",
                    OnSelect: EventCallback.Factory.Create(this, () => { confirmingReturn = true; }),
                    Hint: "Sends every email linked to this request back to the Control Centre queue",
                    Destructive: true, Group: 3));
            if (IsAdmin)
                items.Add(new(Label: "Delete request…",
                    OnSelect: EventCallback.Factory.Create(this, () => { confirmingDelete = true; }),
                    Destructive: true, Group: 3));

            return items;
        }
    }

    private void OpenResponseModal()
    {
        actionError = null;
        responseDraft = record?.ResponseText ?? "";
        respondingOpen = true;
    }

    private void CancelResponseModal()
    {
        if (busy) return;
        respondingOpen = false;
    }

    private async Task SaveResponseFromModal()
    {
        // A recorded response puts the ball back in our court: review it, then close it, raise the
        // variation order quote it calls for, or re-issue.
        await Apply(RequestStatus.NeedsAction, responseDraft);
        if (actionError is null) respondingOpen = false;
    }

    private async Task ReopenFromMenu()
    {
        await ReopenRequest();
    }

    // The status pill's dropdown: Closed has its own flow (the close confirm with its date and
    // the agent gate) so a pill change can never skip its safeguards; the rest apply directly,
    // keeping the recorded response text.
    private async Task ChangeStatusFromPill(RequestStatus status)
    {
        if (record is null || busy || status == record.Status) return;
        switch (status)
        {
            case RequestStatus.Closed:
                OpenCloseConfirm();
                break;
            default:
                await Apply(status, record.ResponseText);
                break;
        }
    }

    // "Create bid packages": marks the RFI as carrying an RFQ, which unlocks the Variation Order
    // Quote (and its bid packages) in the rail.
    private async Task CreateBidPackagesFromMenu()
    {
        await EnableRfq();
    }

    // The close confirm modal: opens with today's date pre-filled so the common case is one click,
    // but the date is editable for closures only recorded after the fact.
    private void OpenCloseConfirm()
    {
        if (record is null || busy) return;
        closeDate = DateTime.Today.ToString("yyyy-MM-dd");
        closeError = null;
        confirmingClose = true;
    }

    private void CancelClose() => confirmingClose = false;

    private async Task PerformClose()
    {
        var closedAt = ParseDate(closeDate);
        if (closedAt is null) { closeError = "A close date is required."; return; }
        if (closedAt.Value.Date > DateTime.Today) { closeError = "The closed date cannot be in the future."; return; }
        confirmingClose = false;
        await CloseRequest(closedAt.Value);
    }

    // Closes the request as at the user-chosen date. Any drafted response is persisted first so
    // it isn't lost.
    private async Task CloseRequest(DateTimeOffset closedAt)
    {
        if (record is null || busy) return;
        actionError = null;

        if (!string.IsNullOrWhiteSpace(responseDraft) && responseDraft.Trim() != (record.ResponseText ?? "").Trim())
            await Apply(RequestStatus.NeedsAction, responseDraft);
        if (record is null) return;

        try
        {
            busy = true;
            var closed = await RequestRegister.CloseAsync(record.RequestId, record.ProjectId, closedAt);
            if (closed)
            {
                record = await RequestRegister.GetAsync(RequestId);
                responseDraft = record?.ResponseText ?? "";
            }
            else
            {
                actionError = "This request no longer exists — refresh the page.";
            }
        }
        catch
        {
            actionError = "Couldn't close the request. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

    // Reopening puts the ball back in our court — the reopened request needs someone to act.
    private async Task ReopenRequest() => await Apply(RequestStatus.NeedsAction, record?.ResponseText);

    // Drops every dialog and inline editor on the page. Used when the page is about to navigate
    // away, so a modal can never be left hanging over the destination during the transition.
    private void CloseOpenDialogs()
    {
        confirmingDelete = false;
        confirmingReturn = false;
        confirmingClose = false;
        respondingOpen = false;
        emailModalOpen = false;
        editingHeader = false;
        editingFacts = false;
        editingDetail = false;
        editingForm = false;
        variationDraftOpen = false;
    }

    // Promotion is the end of this record's triage, not the start of another edit: it mints the RFI
    // reference and the official document, and the next thing anyone does is pick up the next
    // request. So the page closes anything open and returns to the register, where the promoted row
    // now reads as an RFI — rather than leaving the user on a page whose buttons have all changed
    // underneath them. A failure keeps them here with the reason.
    private async Task PromoteToRfi()
    {
        if (record is null || busy) return;
        ladderError = null;
        try
        {
            busy = true;
            var promoted = await RequestRegister.PromoteToRfiAsync(record.RequestId, ProjectId);
            record = promoted;
            responseDraft = promoted.ResponseText ?? "";
            CloseOpenDialogs();
            Nav.NavigateTo($"/projects/{ProjectId}/requests");
        }
        catch
        {
            ladderError = "Couldn't promote the request to an RFI. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

    private async Task EnableRfq()
    {
        if (record is null || busy) return;
        ladderError = null;
        try
        {
            busy = true;
            record = await RequestRegister.EnableRfqAsync(record.RequestId, ProjectId);
            await LoadVariationAsync();
        }
        catch
        {
            ladderError = "Couldn't enable bid packages on this RFI. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

    // The variation draft. The dialog opens INSTANTLY, seeded from the RFI's own title and
    // description, and the assistant fills it in from the panel beside it — an exchange rather than
    // a five-second wait on one blocking call. Nothing here writes: CreateVoqFromRfq commits what
    // the user presses Raise variation on. (Those command names are API identifiers and keep their
    // historic spelling.)
    private bool variationDraftOpen;

    // Whether the form has already been filled in for this page's record. Keyed on the PAGE, not on
    // AiTaskState: ending the conversation (the panel's "Done") must never be the thing that
    // discards a drafted variation the next time the dialog is opened.
    private bool variationDraftSeeded;

    private string? variationDraftError;
    private string draftVariationTitle = "";
    private string draftVariationDescription = "";
    private string draftVariationValue = "";
    private string draftVariationTrade = "";
    private List<DraftLineRow> draftVariationLines = new();

    // Which fields the assistant changed on its last pass, so they pulse rather than silently
    // differing from what the user last read. Cleared on a timer.
    // Storage limits on VariationOrderEntity. Enforced here now that no server-side draft handler
    // sits in front of the dialog to clamp what a model returned.
    private const int MaxVariationTitleChars = 256;
    private const int MaxVariationDescriptionChars = 2048;

}
