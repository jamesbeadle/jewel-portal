using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- Communications: the shared Reply/Forward composer over the tagged-email list ----
    // The email a Reply or Forward was pressed on (the shared composer opens above the list;
    // sending from a record page sends immediately), which of the two it was, and the
    // confirmation left behind by the last send.
    private MailReplyComposer? commsComposer;
    private MailboxMessage? commsReplyTo;
    private bool commsComposeIsForward;
    private string? commsReplySent;

    private void StartCommsCompose(MailboxMessage message, bool forward)
    {
        commsReplyTo = message;
        commsComposeIsForward = forward;
    }

    private void CancelCommsCompose()
    {
        commsReplyTo = null;
    }

    private async Task OnCommsReplySent(Jewel.JPMS.Contracts.MailboxCompose.ComposeOutcome outcome)
    {
        var wasForward = commsComposeIsForward;
        commsReplyTo = null;
        commsComposeIsForward = false;
        commsReplySent = outcome.Sent
            ? $"{(wasForward ? "Forward" : "Reply")} sent to {string.Join("; ", outcome.To)} — it joins the thread and files back into this list."
            : $"The {(wasForward ? "forward" : "reply")} was saved to the mailbox's Drafts — review and send it from Outlook.";
        // The sent copy self-files by tag; re-read so it appears in the list straight away.
        try { fetchedEmails = await Queries.AskAsync(new ListBidPackageEmails(BidPackageId), CancellationToken.None); }
        catch { /* the list simply refreshes on the next full load */ }
    }

    /// <summary>After the Find-emails dialog tags something: re-read so it appears straight away.</summary>
    private async Task ReloadEmailsAsync()
    {
        try { fetchedEmails = await Queries.AskAsync(new ListBidPackageEmails(BidPackageId), CancellationToken.None); }
        catch { /* the list simply refreshes on the next full load */ }
    }

    private bool showInviteModal;
    private string subSearch = "";
    private readonly HashSet<string> selected = new(StringComparer.OrdinalIgnoreCase);

    private bool CanManage => Session.AvailableRoles.Any(r =>
        r is Role.Admin or Role.ManagingDirector or Role.ProjectManager);

    // A closed package is read-only: every action affordance gates on CanEdit, so the record
    // stays as the audit trail of a tender that ran and ended. Close/Reopen themselves gate on
    // CanManage — they are the acts that flip this.
    private bool IsClosed => package?.Status == BidPackageStatus.Closed;
    private bool CanEdit => CanManage && !IsClosed;

    // Who may promote a tender-only prospect into the Directory — mirrors the API's
    // PromoteSubcontractorToDirectoryAuthorisation (directory curators; admins pass everything),
    // so the button never offers an act the server would refuse.
    private bool CanAddToDirectory => Session.AvailableRoles.Any(r =>
        r is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.OfficeComplianceCoordinator or Role.OfficeAdmin);

    // The "Add to directory" act on a submitted tender: promotes a tender-only prospect into the
    // Directory proper. Available even on a closed package — judging a company worth keeping is
    // directory curation, not tender editing.
    private async Task AddToDirectory(string subcontractorId)
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            await Commands.SendAsync(new PromoteSubcontractorToDirectory(subcontractorId), CancellationToken.None);
            await SubsReadModel.RefreshAsync(CancellationToken.None);
        }
        catch (CommandFailedException ex) { error = $"Couldn't add that company to the directory: {ex.Message}"; }
        catch { error = "Couldn't add that company to the directory. Please try again."; }
        finally { busy = false; }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await Session.EnsureLoadedAsync();
            if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
            sessionReady = true;
            Subs.OnChange += OnStoreChanged;
            CostCenters.OnChanged += OnStoreChanged;
            // The project list feeds the reply composer's attachment picker (drawings/photos by
            // project) — revalidated in the background like every other read model here.
            ProjectList.OnChanged += OnStoreChanged;
            _ = LoadProjectListAsync();
            try { _ = Subs.All(); } catch { /* directory load is best-effort */ }
            // Cost centres feed the line-item cost-code selects — best-effort background refresh.
            try { _ = CostCenters.RefreshAsync(CancellationToken.None); } catch { }
            await LoadAsync();
        }
        catch (Exception ex)
        {
            error = $"Couldn't load this page: {ex.Message}";
        }
        finally
        {
            // Set here too: a failure before the session resolved still has to reveal the page,
            // banner and all, rather than leaving it under a spinner.
            sessionReady = true;
            loadAttempted = true;
        }
    }

    public void Dispose()
    {
        Subs.OnChange -= OnStoreChanged;
        CostCenters.OnChanged -= OnStoreChanged;
        ProjectList.OnChanged -= OnStoreChanged;
    }

    // Losing the project list should cost the composer its drawing/photo sources, not the page.
    private async Task LoadProjectListAsync()
    {
        try { await ProjectList.RefreshAsync(CancellationToken.None); }
        catch { /* reported by the query client; the picker's project sources render empty */ }
    }

    private void OnStoreChanged() => InvokeAsync(StateHasChanged);

    // Each piece loads independently so one failing query doesn't take down the whole view.
    private async Task LoadAsync()
    {
        try { package = await Queries.AskAsync(new GetBidPackageById(BidPackageId), CancellationToken.None); }
        catch (Exception ex) { error = $"Couldn't load the bid package: {ex.Message}"; }

        try { fetchedRecipients = await Queries.AskAsync(new ListBidPackageRecipients(BidPackageId), CancellationToken.None); }
        catch (Exception ex) { error = Append(error, $"Couldn't load invited subcontractors: {ex.Message}"); }

        try { fetchedLineItems = await Queries.AskAsync(new ListBidPackageLineItems(BidPackageId), CancellationToken.None); }
        catch (Exception ex) { error = Append(error, $"Couldn't load line items: {ex.Message}"); }

        try { fetchedEmails = await Queries.AskAsync(new ListBidPackageEmails(BidPackageId), CancellationToken.None); }
        catch (Exception ex) { error = Append(error, $"Couldn't load related emails: {ex.Message}"); }

        try { fetchedQuotes = await Queries.AskAsync(new ListQuotesForBidPackage(BidPackageId), CancellationToken.None); }
        catch (Exception ex) { error = Append(error, $"Couldn't load tender submissions: {ex.Message}"); }

        try { fetchedQuoteLines = await Queries.AskAsync(new ListQuoteLineItemsForBidPackage(BidPackageId), CancellationToken.None); }
        catch { /* comparison simply shows totals only */ }

        try { fetchedPackageDrawings = await Queries.AskAsync(new ListBidPackageDrawings(BidPackageId), CancellationToken.None); }
        catch { /* documents section simply shows none */ }

        try { fetchedAttachments = await PackageAttachments.ListAsync(BidPackageId); }
        catch { /* documents section simply shows none */ }

        // The order this package's award raised — drives the persistent award summary. Best-effort:
        // without it the summary simply doesn't render (statuses still show Awarded/Won).
        try { projectOrders = await Queries.AskAsync(new ListProjectWorkOrders(ProjectId), CancellationToken.None); }
        catch { /* award summary simply hidden */ }

        // Coverage targets — best-effort so the picker can offer contract BoQ lines and variations.
        try { boqLines = await Queries.AskAsync(new ListBoqLinesForProject(ProjectId), CancellationToken.None); }
        catch { /* picker simply shows no BoQ lines */ }

        try { variations = await Queries.AskAsync(new ListVariationOrdersForProject(ProjectId), CancellationToken.None); }
        catch { /* picker simply shows no variations */ }

    }


}
