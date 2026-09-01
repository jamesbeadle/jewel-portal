using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Features.Triage.Panels;

public partial class StagedRecordActionEditor
{
    /// <summary>Which record this instance drafts — Request (see <see cref="RequestKind"/>),
    /// BidPackage, WorkOrder or Defect. The pane keys the editor by its action, so a switch
    /// remounts; a staged draft (page-owned) keeps its content and is re-stamped to the new kind.</summary>
    [Parameter] public StagedRecordKind Kind { get; set; } = StagedRecordKind.Request;

    /// <summary>For the Request kind: General ("Raise Request") or Rfi ("Raise RFI").</summary>
    [Parameter] public RequestType RequestKind { get; set; } = RequestType.General;

    /// <summary>The staged new record (null = none). Owned by the page; edited here.</summary>
    [Parameter] public StagedRecordCreate? StagedCreate { get; set; }
    [Parameter] public EventCallback<StagedRecordCreate?> StagedCreateChanged { get; set; }

    /// <summary>The open email's attachments, offered as tick-boxes on the work-order form so
    /// they can be kept on the new order as record keeping.</summary>
    [Parameter] public IReadOnlyList<IntakeAttachment> EmailAttachments { get; set; } = Array.Empty<IntakeAttachment>();

    /// <summary>The selected email's sender — pre-selects the work-order subcontractor when it
    /// matches a directory record, and pre-fills the defect assignee. Suggestions only.</summary>
    [Parameter] public string SenderEmail { get; set; } = "";

    // True while the defect assignee is the sender suggestion rather than a typed value.
    private bool defectAssigneeFromSender;
    private bool workOrderLookupsRequested;
    private string? senderMatchedCompany;

    // The form binds to a scratch instance until something is typed, so an empty form never
    // stages a phantom "new record".
    private StagedRecordCreate Create => StagedCreate ?? scratchCreate;
    private StagedRecordCreate scratchCreate = new();
    // Once staged, the scratch and the page's copy are the SAME object — so when the staged
    // draft is removed (the chip's Remove, or an apply landing) the scratch must be replaced,
    // or the form would keep the old content and silently re-stage it on the next keystroke.
    private StagedRecordCreate? lastStaged;

    protected override void OnParametersSet()
    {
        if (lastStaged is not null && StagedCreate is null)
        {
            scratchCreate = new StagedRecordCreate { ResponseDue = RequestDefaults.ResponseDue() };
            defectAssigneeFromSender = false;
            senderMatchedCompany = null;
            if (Kind == StagedRecordKind.Defect) TryPrefillDefectAssignee();
            if (Kind == StagedRecordKind.WorkOrder) TryMatchSender();
        }
        lastStaged = StagedCreate;
    }

    protected override void OnInitialized()
    {
        // The house-standard response window pre-fills the request form — a week is what all but
        // a handful of requests get, so typing a date is keystrokes spent on the common case.
        scratchCreate = new StagedRecordCreate { ResponseDue = RequestDefaults.ResponseDue() };
        // A draft staged under another action keeps its content — just re-stamp what Apply will
        // raise from it, exactly as the tags pane's Record sub-tabs did.
        if (StagedCreate is { } staged && (staged.Kind != Kind
            || (Kind == StagedRecordKind.Request && staged.RequestKind != RequestKind)))
            NotifyCreate();
        if (Kind == StagedRecordKind.WorkOrder) _ = EnsureWorkOrderLookupsAsync();
        if (Kind == StagedRecordKind.Defect) TryPrefillDefectAssignee();
    }

    private void NotifyCreate()
    {
        // First keystroke on the scratch form stages it; a staged form just reports the edit.
        var target = StagedCreate ?? scratchCreate;
        target.Kind = Kind;
        if (Kind == StagedRecordKind.Request) target.RequestKind = RequestKind;
        _ = StagedCreateChanged.InvokeAsync(target);
    }

    /// <summary>Every work-order form edit funnels through here: apply the edit, stamp the kind,
    /// and stage/report — so a half-built order is always the one staged record.</summary>
    private void SetWorkOrder(Action<StagedRecordCreate> edit)
    {
        edit(Create);
        NotifyCreate();
    }

    /// <summary>Every defect form edit funnels through here — same shape as the work order's. A
    /// user edit of the assignee replaces the sender suggestion, so the hint stops claiming it.</summary>
    private void SetDefect(Action<StagedRecordCreate> edit)
    {
        var suggestedAssignee = Create.DefectAssignedTo;
        edit(Create);
        if (defectAssigneeFromSender && Create.DefectAssignedTo != suggestedAssignee)
            defectAssigneeFromSender = false;
        NotifyCreate();
    }

    /// <summary>Pre-fill the defect's assignee from the email's sender — a suggestion only (never
    /// overrides a value already there), filling the scratch form WITHOUT staging it.</summary>
    private void TryPrefillDefectAssignee()
    {
        if (string.IsNullOrWhiteSpace(SenderEmail) || Create.DefectAssignedTo != "") return;
        Create.DefectAssignedTo = SenderEmail.Trim();
        defectAssigneeFromSender = true;
        // A form already staged reports the fill so the page's copy stays current.
        if (StagedCreate is not null) _ = StagedCreateChanged.InvokeAsync(StagedCreate);
    }

    // One refresh per mount (stale-while-revalidate): cached lists render immediately, the fetch
    // freshens them in the background — and the sender match runs once the directory has landed.
    private async Task EnsureWorkOrderLookupsAsync()
    {
        if (workOrderLookupsRequested)
        {
            TryMatchSender();
            return;
        }
        workOrderLookupsRequested = true;
        try
        {
            await Task.WhenAll(
                Subcontractors.RefreshAsync(CancellationToken.None),
                CostCenters.RefreshAsync(CancellationToken.None));
        }
        catch { /* the pickers fall back to whatever is cached; the error toast already reported */ }
        TryMatchSender();
        StateHasChanged();
    }

    // Sender addresses from the big freemail providers say nothing about the company, so they
    // never drive a domain match (an exact contact-email match still wins even there).
    private static readonly HashSet<string> FreemailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com", "hotmail.com", "hotmail.co.uk", "outlook.com",
        "live.com", "live.co.uk", "yahoo.com", "yahoo.co.uk", "icloud.com", "me.com",
        "aol.com", "msn.com", "btinternet.com", "sky.com"
    };

    /// <summary>Pre-select the subcontractor from the email's sender: an exact contact-email match
    /// first, else the single directory record sharing the sender's (non-freemail) domain. Never
    /// overrides a choice already made.</summary>
    private void TryMatchSender()
    {
        if (string.IsNullOrWhiteSpace(SenderEmail) || Subcontractors.Current is not { } companies) return;
        if (Create.SubcontractorId != "") return;
        var email = SenderEmail.Trim();
        var match = companies.FirstOrDefault(company =>
            string.Equals(company.ContactEmail?.Trim(), email, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            var at = email.LastIndexOf('@');
            if (at < 0) return;
            var domain = email[(at + 1)..];
            if (FreemailDomains.Contains(domain)) return;
            var byDomain = companies
                .Where(company => (company.ContactEmail ?? "").Trim()
                    .EndsWith("@" + domain, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (byDomain.Count != 1) return;
            match = byDomain[0];
        }
        Create.SubcontractorId = match.SubcontractorId;
        senderMatchedCompany = match.CompanyName;
        // A scratch form isn't staged by a suggestion — only by the user's own first edit. A form
        // already staged reports the fill so the page's copy stays current.
        if (StagedCreate is not null) _ = StagedCreateChanged.InvokeAsync(StagedCreate);
    }

    private IEnumerable<Subcontractor> SortedSubcontractors =>
        (Subcontractors.Current ?? Array.Empty<Subcontractor>())
            .OrderBy(company => company.CompanyName, StringComparer.OrdinalIgnoreCase);

    // The work-order form's chosen supplier — what the auto-send warning names, and where the
    // purchase-order email goes when the page's Apply raises a released (non-draft) order.
    private Subcontractor? WorkOrderSupplier =>
        string.IsNullOrWhiteSpace(Create.SubcontractorId)
            ? null
            : (Subcontractors.Current ?? Array.Empty<Subcontractor>()).FirstOrDefault(sub =>
                string.Equals(sub.SubcontractorId, Create.SubcontractorId, StringComparison.OrdinalIgnoreCase));

    // The label carries code + name so typing matches either — same convention as the manual
    // work-order modal and the Xero allocation page.
    private IReadOnlyList<SearchSelect.Option> CostCentreOptions =>
        CostCenters.Alphabetical
            .Select(centre => new SearchSelect.Option(centre.Code, $"{centre.Code} {centre.Name}"))
            .ToList();

    private decimal OrderTotal => Create.Lines.Sum(line => line.Amount ?? 0m);


    // Email attachments that can actually be acted on: legacy snapshots without an id can't be
    // fetched from the mailbox, so they aren't offered.
    private IReadOnlyList<IntakeAttachment> TickableEmailAttachments =>
        (EmailAttachments ?? Array.Empty<IntakeAttachment>())
            .Where(attachment => !string.IsNullOrEmpty(attachment.Id))
            .ToList();

    private void ToggleEmailAttachment(IntakeAttachment attachment, bool ticked) =>
        SetWorkOrder(create =>
        {
            if (ticked)
            {
                if (!create.EmailAttachmentIds.Contains(attachment.Id))
                    create.EmailAttachmentIds.Add(attachment.Id);
            }
            else
            {
                create.EmailAttachmentIds.Remove(attachment.Id);
            }
        });

    private void OnWorkOrderFilesSelected(Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs e) =>
        SetWorkOrder(create => create.UploadFiles.AddRange(e.GetMultipleFiles(20)));

}
