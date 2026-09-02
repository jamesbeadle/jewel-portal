using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string RequestId { get; set; } = "";

    // Session checked and the user is signed in — not "the record is here". The tab chrome shows
    // straight away; `dataLoaded` is the separate question of whether the record has arrived.
    private bool sessionReady;
    private bool dataLoaded;
    private Request? record;

    // ---- Panes ------------------------------------------------------------------------------
    // The request is the containing object; once promoted, the official document (RFI / NOD / EOT)
    // is its own tab. "request" or "official" — the variation tab navigates to that record's page.
    private string activeTab = "request";

    // An unpromoted request (General or any pre-official kind) has no official tab: there is no
    // official document yet, so the prepared-ahead form and the issue machinery sit on the
    // container pane, exactly as they always did. IsEmailable is what "official" means here —
    // the same test the form panel and the tab bar use.
    private bool HasOfficialTab => record is not null && record.Kind.IsEmailable();

    private bool ShowContainerPane => !HasOfficialTab || activeTab != "official";
    private bool ShowOfficialContent => !HasOfficialTab || activeTab == "official";

    private string ActiveTabId => HasOfficialTab && activeTab == "official" ? "official" : "request";

    private void SetActiveTab(string tab) =>
        activeTab = tab == "official" && HasOfficialTab ? "official" : "request";

    // Deep links (the variation page's RFI tab, forwarded links) land on the official pane with
    // ?tab=official. Read once at load; the address bar is not the source of truth thereafter.
    private void HonourRequestedTabFromRoute()
    {
        var query = new Uri(Nav.Uri).Query;
        if (query.Contains("?tab=official", StringComparison.OrdinalIgnoreCase)
            || query.Contains("&tab=official", StringComparison.OrdinalIgnoreCase))
            SetActiveTab("official");
    }
    private string responseDraft = "";
    private bool busy;
    private string? actionError;
    private bool confirmingDelete;
    private bool confirmingReturn;
    private bool respondingOpen;
    private IReadOnlyList<Client> clients = Array.Empty<Client>();
    private IReadOnlyList<Architect> architects = Array.Empty<Architect>();

    private string? ladderError;
    private VariationOrder? variation;
    private string? variationError;

    // Official document form editor (the itemised queries + narrative sections).
    private bool editingForm;

    // Email draft staging (Outlook draft in the projects mailbox) — all lives in the email modal.
    private bool emailModalOpen;
    private bool preparingDraft;
    private string? draftError;
    private RequestEmailDraft? draftResult;
    private RequestRecipientSet? recipientPreview;

    // The emails tagged to this request (inbound legs with a live mailbox id), offered in the
    // modal as chains the official document can be issued into as a reply — newest first.
    // "" = no chain selected = start a fresh email.
    private IReadOnlyList<RequestMessage> taggedEmails = Array.Empty<RequestMessage>();
    private bool taggedEmailsLoading;
    private string selectedChainMailboxId = "";

    private string VariationHref => variation is null ? "" : $"/projects/{ProjectId}/variations/{variation.VariationOrderId}";

    // The reference the page leads with: once promoted, the official instrument number (RFI-014) —
    // the number correspondents know it by — with the REQ container number as secondary context.
    // A General request leads with its REQ-#### container number.
    private string PrimaryReference =>
        record is null ? "" :
        record.Kind != RequestType.General && !string.IsNullOrWhiteSpace(record.Reference)
            ? record.Reference
            : string.IsNullOrEmpty(record.DisplayNumber) ? record.Reference : record.DisplayNumber;

    private bool ShowContainerNumber =>
        record is not null && !string.IsNullOrEmpty(record.DisplayNumber) && record.DisplayNumber != PrimaryReference;

    private string RegisterHref => $"/projects/{ProjectId}/requests";

    // Streams the official PDF from the api, regenerated from SQL on every download.
    private string DocumentHref => $"/api/requests/{RequestId}/document";

    // Mirrors PrepareRequestEmailDraftAuthorisation server-side (directors, project managers, site
    // managers and architects; admins carry every role server-side).
    private bool CanDraftEmail => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager or Role.Architect);

    // Mirrors DeleteRequestAuthorisation server-side: the Admin role only (master administrators
    // carry every role, so they qualify too).
    private bool IsAdmin => Session.AvailableRoles.Any(role => role is Role.Admin);

    // Returning to triage is a triage action, open to administrators, project managers, and the
    // finance director (admins carry every role server-side, so they always satisfy this too).
    private bool CanTriage => Session.AvailableRoles.Any(role => role is Role.Admin or Role.ProjectManager or Role.FinanceDirector);

    // Editing request field values (date issued, references, status, etc.) is open to project
    // managers and administrators. Admins carry every role server-side, so they always qualify.
    private bool CanEditDetails => Session.AvailableRoles.Any(role => role is Role.Admin or Role.ProjectManager);

    // Raising a variation is a wider set than editing the request, and always was on the server:
    // this mirrors VariationRoles.AllowedToManageVariations and ModalCatalog.VariationDraft's
    // OpenableBy exactly. Keeping the button behind CanEditDetails hid it from the MD and the QS
    // while the API would happily have accepted CreateVoqFromRfq from either — and now that the
    // assistant can route someone here with open_modal, a gate narrower than the grant means being
    // told a form has been opened over a page that has no form.
    private bool CanRaiseVariation => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.QuantitySurveyor);

    // Lifecycle order: the ball starts with us (Needs action), goes to the correspondent (Open),
    // may come back needing a variation order quote (Needs variation), then closes.
    private static readonly RequestStatus[] StatusOptions =
    {
        RequestStatus.NeedsAction, RequestStatus.Open,
        RequestStatus.NeedsVariation, RequestStatus.Closed
    };

    // Close confirm modal: the close date defaults to today but is editable, so a request that
    // actually closed days ago can be recorded against the date it really closed (never a future one).
    private bool confirmingClose;
    private string closeDate = "";
    private string? closeError;

    // One flag per edit modal — header (reference/status/subject), facts strip, Detail panel.
    // The edit* fields below are shared scratch state; each Open* seeds the ones its modal uses,
    // and only one modal is ever open at a time.
    private bool editingHeader;
    private bool editingFacts;
    private bool editingDetail;

    protected override async Task OnInitializedAsync()
    {
        CostCenters.OnChanged += Repaint;
        Activity.OnChanged += Repaint;

        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        sessionReady = true;
        // Activity dots on the tab bar land in the background — absent until then (never gated).
        Activity.Refresh(ProjectId);
        // Two waves instead of six sequential round-trips. Everything in the first wave is
        // independent — the record, the pickers and the variation all key off the route values,
        // not off each other — so they go out together and the page waits once for the slowest
        // rather than for the sum. Only the recipient preview genuinely needs the loaded record,
        // so it follows.
        var firstWave = new List<Task> { LoadAsync(), LoadVariationAsync() };
        if (CanEditDetails)
        {
            firstWave.Add(LoadClientsAsync());
            firstWave.Add(LoadArchitectsAsync());
        }
        await Task.WhenAll(firstWave);

        await LoadRecipientPreviewAsync();
        HonourRequestedTabFromRoute();
        dataLoaded = true;
    }

    private void Repaint() => InvokeAsync(StateHasChanged);

    // The tab bar's per-record activity dots (the request's own linked mail, the variation's, each
    // bid package's) — one lookup shared by every tab.
    private RecordActivitySummary? TabActivity(RecordType type, string recordId) =>
        Activity.For(ProjectId, type, recordId);

    public void Dispose()
    {
        CostCenters.OnChanged -= Repaint;
        Activity.OnChanged -= Repaint;
    }

    // Each of these swallows its own failure so one unavailable picker can never stop the page
    // rendering — the same behaviour as when these ran one after another, just concurrently.
    private async Task LoadClientsAsync()
    {
        try { clients = await ClientStore.ListAsync(); } catch { clients = Array.Empty<Client>(); }
    }

    private async Task LoadArchitectsAsync()
    {
        try { architects = await ArchitectStore.ListAsync(); } catch { architects = Array.Empty<Architect>(); }
    }

    // The tagged emails offered as reply targets in the email modal, fetched fresh each time the
    // modal opens. Best-effort: the modal just shows its empty state on error.
    private async Task LoadTaggedEmailsAsync()
    {
        if (record is null || !record.Kind.IsEmailable() || !CanDraftEmail)
        {
            taggedEmails = Array.Empty<RequestMessage>();
            return;
        }
        try
        {
            taggedEmailsLoading = true;
            // Any live email leg (MailboxId set) can anchor a reply — including the mailbox's own
            // sent copy, where reply-all correctly re-addresses the original recipients.
            taggedEmails = (await RequestRegister.ListMessagesAsync(record.RequestId))
                .Where(m => !string.IsNullOrEmpty(m.MailboxId))
                .OrderByDescending(m => m.PostedAt)
                .ToList();
        }
        catch
        {
            taggedEmails = Array.Empty<RequestMessage>();
        }
        finally
        {
            taggedEmailsLoading = false;
        }
    }

    // The resolved To/CC/BCC an issue or draft would use right now — same resolver as the send
    // paths, refreshed whenever the linked party changes. Best-effort: the panel just hides on error.
    private async Task LoadRecipientPreviewAsync()
    {
        try { recipientPreview = record is null ? null : await Correspondence.ResolveRequestRecipientsAsync(record.RequestId); }
        catch { recipientPreview = null; }
    }

    private async Task LoadVariationAsync()
    {
        // A variation normally appears only after the RFQ step (HasRfq), but one can also be attached
        // to this request through the register's "Link…" repair — those links can predate the
        // flag, so always look for one rather than gating on HasRfq. Returns null when none.
        try { variation = await Variations.GetByRequestAsync(RequestId); }
        catch { variation = null; }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!dataLoaded) return;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        record = await RequestRegister.GetAsync(RequestId);
        responseDraft = record?.ResponseText ?? "";
    }

    private void OnResponseDraftInput(ChangeEventArgs e) => responseDraft = e.Value?.ToString() ?? "";

}
