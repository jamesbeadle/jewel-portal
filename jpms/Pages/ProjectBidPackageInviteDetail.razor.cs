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
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string BidPackageId { get; set; } = "";

    // Session checked and the user is signed in — not "the package is here". The tab chrome and
    // the back link show straight away; each section waits behind its own gate.
    private bool sessionReady;

    // ---- Section tabs (Details leads) — local panes, the request page's pattern. ----

    private string activeTab = "details";

    private static readonly (string Key, string Label)[] SectionTabs =
    {
        // Details leads and holds BOTH the specification summary and the line items — they are
        // one act of authorship, and splitting them across tabs is what broke the AI flow's
        // follow-through (2026-08-16).
        ("details", "Details"),
        ("tender-list", "Tender list"),
        ("submissions", "Submissions"),
        ("documents", "Documents"),
        ("emails", "Emails"),
    };

    // The chip classes the RFIs register uses for its document-type tabs.
    private string TabClass(string key) => key == activeTab
        ? "px-3 py-1.5 rounded-md bg-accent text-accent-ink font-medium"
        : "px-3 py-1.5 rounded-md text-content-muted hover:text-content hover:bg-surface-raised";

    // ---- The Actions menu (header) --------------------------------------------------------------

    private IReadOnlyList<DropdownMenu.Item> HeaderActions()
    {
        var items = new List<DropdownMenu.Item>();
        items.Add(IsClosed
            ? new DropdownMenu.Item("Reopen package",
                OnSelect: EventCallback.Factory.Create(this, ReopenPackage),
                Hint: "Puts the tender back in play.", Group: 1)
            : new DropdownMenu.Item("Close package",
                OnSelect: EventCallback.Factory.Create(this, ClosePackage),
                Hint: "Ends the tender with no winner selected — the polite ending for a real tender.", Group: 1));
        items.Add(new DropdownMenu.Item("Delete package…",
            OnSelect: EventCallback.Factory.Create(this, OpenDeleteModal),
            Hint: "Removes the package and its tender data for good.",
            Destructive: true, Group: 2));
        return items;
    }

    // Every query in LoadAsync has had its turn. Some may have failed — that is the point: a gate
    // held open by a fetch that is never coming back is worse than an empty panel.
    private bool loadAttempted;
    private bool busy;
    private string? error;

    private BidPackage? package;
    // Nullable on purpose: every one of these lists has a real empty answer ("nobody invited yet",
    // "no drawings linked"), so "not fetched" has to be a state of its own or each panel announces
    // an emptiness it hasn't checked. The lowercase accessors keep the reads non-null.
    private IReadOnlyList<BidPackageRecipient>? fetchedRecipients;
    private IReadOnlyList<BidPackageLineItem>? fetchedLineItems;
    private IReadOnlyList<MailboxMessage>? fetchedEmails;
    private IReadOnlyList<Quote>? fetchedQuotes;
    private IReadOnlyList<QuoteLineItem>? fetchedQuoteLines;
    private IReadOnlyList<Drawing>? fetchedPackageDrawings;
    private IReadOnlyList<BidPackageAttachment>? fetchedAttachments;

    private IReadOnlyList<BidPackageRecipient> recipients => fetchedRecipients ?? Array.Empty<BidPackageRecipient>();
    private IReadOnlyList<BidPackageLineItem> lineItems => fetchedLineItems ?? Array.Empty<BidPackageLineItem>();
    private IReadOnlyList<MailboxMessage> emails => fetchedEmails ?? Array.Empty<MailboxMessage>();
    private IReadOnlyList<Quote> quotes => fetchedQuotes ?? Array.Empty<Quote>();
    private IReadOnlyList<QuoteLineItem> quoteLines => fetchedQuoteLines ?? Array.Empty<QuoteLineItem>();
    private IReadOnlyList<Drawing> packageDrawings => fetchedPackageDrawings ?? Array.Empty<Drawing>();
    private IReadOnlyList<BidPackageAttachment> packageAttachments => fetchedAttachments ?? Array.Empty<BidPackageAttachment>();

    private IReadOnlyList<ProjectWorkOrderDetail> projectOrders = Array.Empty<ProjectWorkOrderDetail>();
    private IReadOnlyList<BoqLineItem> boqLines = Array.Empty<BoqLineItem>();
    private IReadOnlyList<VariationOrder> variations = Array.Empty<VariationOrder>();

    // ── Panel gates. A query that failed has "arrived" as far as the gate is concerned: the
    // banner at the top says what went wrong, and a jewel that pulses for ever says nothing. ──
    private bool RecipientsReady => fetchedRecipients is not null || loadAttempted;
    private bool LineItemsReady => fetchedLineItems is not null || loadAttempted;
    private bool EmailsReady => fetchedEmails is not null || loadAttempted;
    // The comparison table reads the quote lines alongside the quotes, so it waits for both.
    private bool QuotesReady => (fetchedQuotes is not null && fetchedQuoteLines is not null) || loadAttempted;
    // The Documents panel reads drawings AND uploaded attachments — it reveals in one piece.
    private bool DrawingsReady => (fetchedPackageDrawings is not null && fetchedAttachments is not null) || loadAttempted;

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


    // ---- Line-item coverage (link to a cost centre or a variation order) ----

    private bool showCoverageModal;
    private BidPackageLineItem? linkingLine;
    private BidPackageLineCoverage coverageChoice = BidPackageLineCoverage.Unassigned;
    private string? coverageCostCode;
    private string? coverageVariationId;

    private string CoverageLabel(BidPackageLineItem item)
    {
        switch (item.Coverage)
        {
            case BidPackageLineCoverage.ContractLine:
                // Legacy BoQ links (retired 2026-08-16) keep displaying what they meant.
                if (!string.IsNullOrWhiteSpace(item.BoqLineItemId))
                {
                    var boq = boqLines.FirstOrDefault(b => b.BoqLineItemId == item.BoqLineItemId);
                    return boq is null ? "Contract line" : $"BoQ · {boq.Description}";
                }
                return string.IsNullOrWhiteSpace(item.CostCode) ? "Cost centre" : $"CC · {item.CostCode}";
            case BidPackageLineCoverage.Variation:
                var variation = variations.FirstOrDefault(v => v.VariationOrderId == item.VariationOrderId);
                return variation is null ? "Variation" : $"{variation.DisplayNumber} · {variation.Title}";
            default:
                return "";
        }
    }

    private void OpenCoverageModal(BidPackageLineItem item)
    {
        linkingLine = item;
        coverageChoice = item.Coverage;
        coverageCostCode = item.CostCode;
        coverageVariationId = item.VariationOrderId;
        showCoverageModal = true;
    }

    private void CloseCoverageModal()
    {
        showCoverageModal = false;
        linkingLine = null;
    }

    private async Task ConfirmCoverage()
    {
        if (busy || linkingLine is null || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            var costCode = coverageChoice == BidPackageLineCoverage.ContractLine && !string.IsNullOrWhiteSpace(coverageCostCode) ? coverageCostCode : null;
            var variationId = coverageChoice == BidPackageLineCoverage.Variation && !string.IsNullOrWhiteSpace(coverageVariationId) ? coverageVariationId : null;
            fetchedLineItems = await Commands.SendAsync(
                new SetBidPackageLineItemCoverage(linkingLine.LineItemId, coverageChoice,
                    BoqLineItemId: null, VariationOrderId: variationId, CostCode: costCode), CancellationToken.None);
            showCoverageModal = false;
            linkingLine = null;
        }
        catch { error = "Couldn't update coverage. Make sure a cost centre or variation order is selected, then try again."; }
        finally { busy = false; }
    }

    private static string Append(string? existing, string line) =>
        string.IsNullOrEmpty(existing) ? line : existing + "\n" + line;

    // ---- Invite (modal over the directory) ----

    private string? tradeFilter;

    private void OpenInviteModal()
    {
        // Same readiness gate as the button that opens this — belt for a stale render.
        if (!PackageReadyForInvites) return;
        selected.Clear();
        subSearch = "";
        // Trade is required by the directory — prefill with the package's trade so quick-add just works.
        if (string.IsNullOrWhiteSpace(quickAddTradeId))
            quickAddTradeId = CuratedTradeIdFor(package?.Trade) ?? "";
        // Pre-filter to the package's trade when the directory knows it — one less tap.
        tradeFilter = InvitableTrades()
            .FirstOrDefault(t => string.Equals(t, package?.Trade, StringComparison.OrdinalIgnoreCase));
        showInviteModal = true;
    }

    // The distinct trade names among companies that could be invited (never clients/architects,
    // never tender-only prospects — the picker is the curated directory).
    // A company with several trades appears under each of them.
    private IReadOnlyList<string> InvitableTrades() =>
        Subs.All()
            .Where(s => !s.IsProspect)
            .Where(s => s.Category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier)
            .SelectMany(s => s.Trades)
            .Select(t => t.Name.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

    // The curated trade id for a trade name, when the list knows it.
    private string? CuratedTradeIdFor(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null
        : Subs.Trades().FirstOrDefault(t => string.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))?.TradeId;

    // Resolves a trade name to a curated trade id, adding it to the curated list if new — the
    // server normalises and de-duplicates, so this never mints case-variant duplicates.
    private async Task<string> EnsureTradeIdAsync(string? name)
    {
        var trimmed = string.IsNullOrWhiteSpace(name) ? "General" : name.Trim();
        if (CuratedTradeIdFor(trimmed) is string existing) return existing;
        var created = await Commands.SendAsync(new AddTrade(trimmed), CancellationToken.None);
        return created.TradeId;
    }

    private void CloseInviteModal() => showInviteModal = false;

    private void OnSearchInput(ChangeEventArgs e) => subSearch = e.Value?.ToString() ?? "";

    private IReadOnlyList<Subcontractor> Invitable()
    {
        var invited = recipients.Select(r => r.SubcontractorId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var q = (subSearch ?? "").Trim();
        return Subs.All()
            // Only companies we tender to — never clients or architects, and never tender-only
            // prospects: the picker offers the curated directory, prospects are re-found via the
            // local search (which reuses their record rather than duplicating it).
            .Where(s => s.Category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier)
            .Where(s => !s.IsProspect)
            .Where(s => !invited.Contains(s.SubcontractorId))
            // Any of the company's trades counts — "Boarding" and "Plastering" both surface a
            // company that carries both, where the old free-text compound string never matched.
            .Where(s => tradeFilter is null || s.Trades.Any(t => string.Equals(t.Name.Trim(), tradeFilter, StringComparison.OrdinalIgnoreCase)))
            .Where(s => q.Length == 0
                || (s.CompanyName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                || s.Trades.Any(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                || (s.ContactName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void Toggle(string subcontractorId, ChangeEventArgs e)
    {
        if (e.Value is true) selected.Add(subcontractorId);
        else selected.Remove(subcontractorId);
    }

    // ---- Quick-add: invite someone who isn't in the directory yet ----

    private string quickAddName = "";
    private string quickAddEmail = "";
    private string quickAddTradeId = "";

    private bool QuickAddReady =>
        !string.IsNullOrWhiteSpace(quickAddName)
        && quickAddEmail.Contains('@') && quickAddEmail.Trim().Length >= 5;

    // Something typed but not enough to include — surfaced so a half-filled row isn't silently dropped.
    private bool quickAddPartial =>
        !QuickAddReady && (!string.IsNullOrWhiteSpace(quickAddName) || !string.IsNullOrWhiteSpace(quickAddEmail));

    private int InviteCount() => selected.Count + (QuickAddReady ? 1 : 0);

    private async Task ConfirmInvite()
    {
        if (busy || InviteCount() == 0 || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            var ids = selected.ToList();
            if (QuickAddReady)
            {
                // Save the ad-hoc contact as a tender-only PROSPECT (not a directory entry), then
                // invite like any other. The record requires a trade — fall back to the package's,
                // then "General". They join the Directory only if promoted from a submitted tender
                // (or by winning the package) — quality is judged on the tender, not the invite.
                var tradeId = !string.IsNullOrWhiteSpace(quickAddTradeId) ? quickAddTradeId
                    : await EnsureTradeIdAsync(package?.Trade);
                var created = await Commands.SendAsync(
                    new AddSubcontractorToDirectory(
                        quickAddName.Trim(), new[] { tradeId }, quickAddName.Trim(),
                        quickAddEmail.Trim(), "", "", IsProspect: true), CancellationToken.None);
                ids.Add(created.SubcontractorId);
                await SubsReadModel.RefreshAsync(CancellationToken.None);
            }
            fetchedRecipients = await Commands.SendAsync(
                new InviteSubcontractorsToBidPackage(BidPackageId, ids), CancellationToken.None);
            selected.Clear();
            quickAddName = quickAddEmail = quickAddTradeId = "";
            showInviteModal = false;
            package = await Queries.AskAsync(new GetBidPackageById(BidPackageId), CancellationToken.None);
        }
        catch (CommandFailedException ex) { error = $"Couldn't send those invites: {ex.Message}"; }
        catch { error = "Couldn't send those invites. Please try again."; }
        finally { busy = false; }
    }

    // ---- Find local subcontractors (Google Places search near the project's site) ----

    private bool showFindModal;
    private bool findBusy;
    private bool findSearched;
    private bool findResolving;
    private string findTrade = "";
    private string? findError;
    private string? findTradeNote;
    private string? findNotReadyReason;
    private string? findNextPageToken;
    private readonly List<LocalSubcontractor> findResults = new();
    private readonly Dictionary<string, LocalSubcontractor> selectedPlaces = new(StringComparer.Ordinal);

    // Inviting subcontractors is gated on the package actually saying what it is: a title plus
    // details (a specification summary or line items). The details are what the invite email, the
    // pricing schedule and the AI trade match all work from — inviting before they exist sends
    // people a tender for nothing. Mirrors the server's rule in ResolveBidPackageTradeHandler:
    // the buttons stand down here, the endpoint refuses there, so the gate holds both ways round.
    private bool PackageReadyForInvites =>
        !string.IsNullOrWhiteSpace(package?.Title)
        && (!string.IsNullOrWhiteSpace(package?.SpecificationSummary) || lineItems.Count > 0);

    // Opens the modal and resolves the search trade FROM the package (title + details, one cheap
    // AI call) instead of asking the user to pick one — then runs the search itself. The resolved
    // term lands in an editable field, so a wrong guess costs one correction. Every failure path
    // degrades to the package's own stored trade rather than stranding the modal.
    private async Task OpenFindModalAsync()
    {
        findTrade = "";
        findResults.Clear();
        selectedPlaces.Clear();
        findNextPageToken = null;
        findError = null;
        findSearched = false;
        findTradeNote = null;
        findNotReadyReason = null;
        showFindModal = true;
        findResolving = true;
        StateHasChanged();

        try
        {
            var resolution = await Queries.AskAsync(
                new ResolveBidPackageTrade(BidPackageId), CancellationToken.None);
            if (resolution is null || !resolution.Ready)
            {
                findNotReadyReason = resolution?.Reason
                    ?? "This package needs its details (under Details) before subcontractors are invited.";
            }
            else
            {
                findTrade = resolution.Trade ?? package?.Trade ?? "";
                findTradeNote = resolution.UsedAi
                    ? "Worked out from the package's title and details — edit it and press Search if it's off."
                    : resolution.Reason;
            }
        }
        catch
        {
            findTrade = package?.Trade ?? "";
            findTradeNote = "The trade couldn't be worked out just now — using the package's own trade.";
        }
        finally
        {
            findResolving = false;
        }
        // Repaint BEFORE the auto-search: Blazor only re-renders an async handler at its first
        // yield and its end, so without this the modal keeps saying "Working out the trade…"
        // through the whole web search — which reads as a hang.
        StateHasChanged();

        if (findNotReadyReason is null && !string.IsNullOrWhiteSpace(findTrade))
            await RunFindSearch(loadMore: false);
        StateHasChanged();
    }

    private void CloseFindModal() => showFindModal = false;

    private async Task RunFindSearch(bool loadMore)
    {
        if (findBusy || string.IsNullOrWhiteSpace(findTrade)) return;
        findBusy = true;
        findError = null;
        // Explicit repaint so the button's spinner shows even when this is called mid-handler
        // (the auto-search after trade resolution) rather than as its own click event.
        StateHasChanged();
        try
        {
            var result = await Queries.AskAsync(
                new SearchLocalSubcontractors(ProjectId, findTrade.Trim(), loadMore ? findNextPageToken : null),
                CancellationToken.None);
            if (!loadMore)
            {
                findResults.Clear();
                selectedPlaces.Clear();
            }
            if (result.Error is not null)
            {
                findError = result.Error;
                findNextPageToken = null;
            }
            else
            {
                findResults.AddRange(result.Results.Where(hit => findResults.All(existing => existing.PlaceId != hit.PlaceId)));
                findNextPageToken = result.NextPageToken;
            }
            findSearched = true;
        }
        catch { findError = "The search failed. Please try again."; }
        finally { findBusy = false; }
    }

    private void TogglePlace(LocalSubcontractor place, ChangeEventArgs e)
    {
        if (e.Value is true) selectedPlaces[place.PlaceId] = place;
        else selectedPlaces.Remove(place.PlaceId);
    }

    // Save each ticked company as a tender-only prospect (unless the directory already knows it),
    // then invite them all. Prospects stay OUT of the Directory until promoted from a submitted
    // tender or by winning the package — only companies judged worth keeping get added.
    private async Task ConfirmFindInvite()
    {
        if (busy || selectedPlaces.Count == 0 || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            var ids = new List<string>();
            var directoryChanged = false;
            foreach (var place in selectedPlaces.Values)
            {
                if (!string.IsNullOrEmpty(place.ExistingSubcontractorId))
                {
                    ids.Add(place.ExistingSubcontractorId);
                    // Backfill contact details the search discovered but the directory entry lacks
                    // (e.g. entries added before email discovery existed). Blanks only — a value
                    // someone typed into the directory deliberately is never overwritten.
                    var existing = Subs.Find(place.ExistingSubcontractorId);
                    var needsEmail = existing is not null
                        && string.IsNullOrWhiteSpace(existing.ContactEmail) && !string.IsNullOrWhiteSpace(place.Email);
                    var needsPhone = existing is not null
                        && string.IsNullOrWhiteSpace(existing.ContactPhone) && !string.IsNullOrWhiteSpace(place.Phone);
                    if (needsEmail || needsPhone)
                    {
                        await Commands.SendAsync(
                            new UpdateSubcontractor(
                                existing!.SubcontractorId, existing.CompanyName, existing.Trades.Select(t => t.TradeId).ToList(), existing.ContactName,
                                needsEmail ? place.Email! : existing.ContactEmail,
                                needsPhone ? place.Phone! : existing.ContactPhone,
                                existing.CisStatus),
                            CancellationToken.None);
                        directoryChanged = true; // refresh the read model so the new email shows immediately
                    }
                    continue;
                }
                var created = await Commands.SendAsync(
                    new AddSubcontractorToDirectory(
                        place.Name, new[] { await EnsureTradeIdAsync(findTrade) }, "", place.Email ?? "", place.Phone ?? "", "",
                        DirectoryCategory.Subcontractor, "", "", "", place.Website ?? "", IsProspect: true),
                    CancellationToken.None);
                ids.Add(created.SubcontractorId);
                directoryChanged = true;
            }
            if (directoryChanged) await SubsReadModel.RefreshAsync(CancellationToken.None);

            fetchedRecipients = await Commands.SendAsync(
                new InviteSubcontractorsToBidPackage(BidPackageId, ids), CancellationToken.None);
            selectedPlaces.Clear();
            showFindModal = false;
            package = await Queries.AskAsync(new GetBidPackageById(BidPackageId), CancellationToken.None);
            sendNote = $"Added {ids.Count} compan{(ids.Count == 1 ? "y" : "ies")} from the local search to the tender list — nothing has been emailed, and they are NOT in the Directory (add the good ones from their submitted tenders). Their addresses were picked up from their websites, so worth a quick check on the list above; when you're ready, compose the Invite email and it sends when you press Send.";
            draftWebLink = null;
        }
        catch (CommandFailedException ex) { error = $"Couldn't invite those companies: {ex.Message}"; }
        catch { error = "Couldn't invite those companies. Please try again."; }
        finally { busy = false; }
    }

    private async Task RemoveRecipient(BidPackageRecipient recipient)
    {
        if (busy || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            fetchedRecipients = await Commands.SendAsync(
                new RemoveBidPackageRecipient(BidPackageId, recipient.RecipientId), CancellationToken.None);
        }
        catch { error = "Couldn't remove that subcontractor. Please try again."; }
        finally { busy = false; }
    }

    // ---- Decline: record "not tendering" without losing them from the invite list ----

    private async Task SetDeclined(BidPackageRecipient recipient, bool declined)
    {
        if (busy || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            fetchedRecipients = await Commands.SendAsync(
                new DeclineBidPackageRecipient(BidPackageId, recipient.RecipientId, declined), CancellationToken.None);
        }
        catch { error = "Couldn't update that subcontractor's status. Please try again."; }
        finally { busy = false; }
    }

    // ---- Link project drawings to the package ----

    private bool showDrawingsModal;
    private IReadOnlyList<Drawing> projectDrawings = Array.Empty<Drawing>();
    private readonly HashSet<string> selectedDrawingIds = new(StringComparer.OrdinalIgnoreCase);

    private async Task OpenDrawingsModal()
    {
        selectedDrawingIds.Clear();
        foreach (var drawing in packageDrawings) selectedDrawingIds.Add(drawing.DrawingId);
        showDrawingsModal = true;
        try { projectDrawings = await Queries.AskAsync(new ListDrawingsForProject(ProjectId), CancellationToken.None); }
        catch { projectDrawings = Array.Empty<Drawing>(); }
    }

    private void CloseDrawingsModal() => showDrawingsModal = false;

    private void ToggleDrawing(string drawingId, ChangeEventArgs e)
    {
        if (e.Value is true) selectedDrawingIds.Add(drawingId);
        else selectedDrawingIds.Remove(drawingId);
    }

    private async Task ConfirmDrawings()
    {
        if (busy || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            fetchedPackageDrawings = await Commands.SendAsync(
                new SetBidPackageDrawings(BidPackageId, selectedDrawingIds.ToList()), CancellationToken.None);
            showDrawingsModal = false;
        }
        catch { error = "Couldn't update the linked drawings. Please try again."; }
        finally { busy = false; }
    }

    // ---- The invite composer: compose, persist on the package, send from the mailbox ----

    private bool showComposeModal;
    private bool editingBody;
    private bool savingDraft;
    private string composeSubject = "";
    private string composeBody = "";
    private string composeTo = "";
    private string composeCc = "";
    private string composeBcc = "";
    private DateTimeOffset? composeDraftSavedAt;
    private string? sendNote;
    private string? draftWebLink;

    // Invited but unreachable — no email address in the directory.
    private IReadOnlyList<string> MissingEmail() =>
        recipients
            .Select(r => Subs.Find(r.SubcontractorId) is { } s
                ? (Name: s.CompanyName, HasEmail: !string.IsNullOrWhiteSpace(s.ContactEmail))
                : (Name: r.SubcontractorId, HasEmail: false))
            .Where(x => !x.HasEmail)
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // The tender list's directory emails — the BCC the composer opens with.
    private IReadOnlyList<string> TenderListEmails() =>
        recipients
            .Select(r => Subs.Find(r.SubcontractorId))
            .Where(s => s is not null && !string.IsNullOrWhiteSpace(s.ContactEmail))
            .Select(s => s!.ContactEmail.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static int CountAddresses(string raw) =>
        raw.Split(new[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Count(address => address.Contains('@'));

    private int ComposeRecipientCount() =>
        CountAddresses(composeTo) + CountAddresses(composeCc) + CountAddresses(composeBcc);

    private async Task OpenComposeModal()
    {
        if (package is null) return;
        sendNote = null;
        editingBody = false;
        composeDraftSavedAt = null;

        // The persisted draft wins — a half-written invite picked up where it was left, by
        // whoever picks it up. Only when none exists does the composer open on the defaults.
        BidPackageInviteComposerDraft? saved = null;
        try { saved = await Queries.AskAsync(new GetBidPackageInviteComposerDraft(BidPackageId), CancellationToken.None); }
        catch { /* no draft is a fine answer; the defaults stand */ }

        if (saved is not null)
        {
            composeSubject = saved.Subject;
            composeBody = saved.Body;
            composeTo = saved.To;
            composeCc = saved.Cc;
            composeBcc = saved.Bcc;
            composeDraftSavedAt = saved.SavedAt;
        }
        else
        {
            composeSubject = $"Invitation to tender — {package.Title} ({package.Reference})";
            composeBody = DefaultInviteBody();
            composeTo = "";
            composeCc = "";
            composeBcc = string.Join("; ", TenderListEmails());
        }
        showComposeModal = true;
        StateHasChanged();
    }

    private async Task SaveInviteDraft()
    {
        if (savingDraft || package is null || !CanEdit) return;
        try
        {
            savingDraft = true;
            await Commands.SendAsync(
                new SaveBidPackageInviteComposerDraft(BidPackageId, composeSubject.Trim(), composeBody,
                    composeTo.Trim(), composeCc.Trim(), composeBcc.Trim()), CancellationToken.None);
            composeDraftSavedAt = DateTimeOffset.Now;
        }
        catch { error = "Couldn't save the invite draft. Please try again."; }
        finally { savingDraft = false; }
    }

    private void CloseComposeModal()
    {
        if (busy) return;
        showComposeModal = false;
        // Closing keeps the work: the draft persists on the package quietly, best-effort — the
        // user asked for exactly this ("it will be useful later"). A failed save costs a re-type,
        // never an error dialog on the way out.
        if (CanEdit && package is not null)
            _ = Commands.SendAsync(
                new SaveBidPackageInviteComposerDraft(BidPackageId, composeSubject.Trim(), composeBody,
                    composeTo.Trim(), composeCc.Trim(), composeBcc.Trim()), CancellationToken.None);
    }

    // The pre-filled HTML invite: scope summary plus the line items to price, grouped by trade.
    private string DefaultInviteBody()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<p>Hello,</p>");
        if (lineItems.Count > 0)
            sb.AppendLine($"<p>Jewel Bespoke Build invites you to tender for the <strong>{package!.Title}</strong> package (ref {package.Reference}). Please complete and return the attached pricing schedule — the Rate and Total columns are left for you — and reply to this email with your exclusions and lead times, quoting the reference.</p>");
        else
            sb.AppendLine($"<p>Jewel Bespoke Build invites you to tender for the <strong>{package!.Title}</strong> package (ref {package.Reference}). Please price the works described in the attached documents and reply to this email with your rates, exclusions and lead times, quoting the reference in your reply.</p>");
        foreach (var group in lineItems.GroupBy(item => item.Trade).OrderBy(g => g.Key))
        {
            var heading = string.IsNullOrWhiteSpace(group.Key) ? "General" : group.Key;
            sb.AppendLine($"<p><strong>{heading}</strong></p>");
            sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
            sb.AppendLine("<tr><th align=\"left\">Description</th><th align=\"left\">Qty</th><th align=\"left\">Unit</th><th align=\"left\">Rate</th><th align=\"left\">Total</th></tr>");
            foreach (var item in group.OrderBy(i => i.SortOrder))
                sb.AppendLine($"<tr><td>{item.Description}</td><td>{item.Quantity}</td><td>{item.Unit}</td><td></td><td></td></tr>");
            sb.AppendLine("</table>");
        }
        sb.AppendLine("<p><strong>Please include the following with your tender:</strong></p>");
        sb.AppendLine("<ul>");
        if (package!.MaterialsApplicable)
            sb.AppendLine("<li><strong>Materials</strong> — please state whether you will be supplying your own materials for this work. If your rates are supply-and-fit, itemise the materials included; if they are labour-only, state what you expect us to supply.</li>");
        sb.AppendLine("<li><strong>Deposit</strong> — our preference is not to pay a deposit upfront. If one is required for you to take on the work, please state the amount and terms in your tender.</li>");
        sb.AppendLine("<li><strong>Duration</strong> — how long the work will take.</li>");
        sb.AppendLine("<li><strong>Availability</strong> — if selected, when you would be able to start.</li>");
        sb.AppendLine("<li><strong>Insurances</strong> — confirmation that you hold all insurances required for this work.</li>");
        sb.AppendLine("<li><strong>RAMS</strong> — confirmation that you will provide the required RAMS documentation.</li>");
        sb.AppendLine("<li><strong>Portfolio</strong> — examples of prior comparable work.</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("<p>Our tender terms and conditions are attached; your tender will be taken as made on that basis unless it states otherwise.</p>");
        sb.AppendLine("<p>Please confirm receipt and let us know if you need any further information or drawings.</p>");
        sb.AppendLine("<p>Kind regards,<br/>Jewel Bespoke Build</p>");
        return sb.ToString();
    }

    private async Task ConfirmSendInvite()
    {
        if (busy || package is null || !CanEdit || ComposeRecipientCount() == 0) return;
        error = null;
        try
        {
            busy = true;
            var outcome = await Commands.SendAsync(
                new SendBidPackageInvite(BidPackageId, composeSubject.Trim(), composeBody,
                    composeTo.Trim(), composeCc.Trim(), composeBcc.Trim()), CancellationToken.None);
            package = outcome.Package;
            draftWebLink = outcome.WebLink;

            if (outcome.Sent)
            {
                showComposeModal = false;
                composeDraftSavedAt = null;
                sendNote = $"Invite sent from the projects mailbox to {outcome.RecipientCount} recipient{(outcome.RecipientCount == 1 ? "" : "s")}, tagged {package.Reference} — replies land under Tender responses.";
                if (outcome.LinkedFiles is { Count: > 0 } linked)
                {
                    sendNote += linked.Count == 1
                        ? $" 1 file was too large to attach and travels as a 7-day download link: {linked[0]}."
                        : $" {linked.Count} files were too large to attach and travel as 7-day download links: {string.Join(", ", linked)}.";
                }
                // The sent copy appears in the Emails tab as the mailbox catches up.
                _ = ReloadEmailsAsync();
            }
            else
            {
                // Staged but not sent — the email survives in Drafts; say so where the user is.
                sendNote = outcome.FailureNote ?? "The send didn't go through — the invite is saved as a draft in the projects mailbox.";
            }
        }
        catch (CommandFailedException ex) { error = $"Couldn't send the invite: {ex.Message}"; }
        catch { error = "Couldn't send the invite. Check the recipients and the mailbox connection, then try again."; }
        finally { busy = false; }
    }

    // ---- Record a tender submission, review, save as a quote ----

    private sealed class ExtractDraft
    {
        public string? BidPackageLineItemId { get; set; }
        public string Description { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Total { get; set; }
    }

    private bool showExtractModal;
    private bool extractBusy;
    private bool extractProposed;
    private bool extractComplete;
    private MailboxMessage? extractSourceEmail;
    private List<string> extractIssues = new();
    private string extractSubcontractorNote = "";
    private string extractSubcontractorId = "";
    private string extractNotes = "";
    private List<ExtractDraft> extractDrafts = new();
    private string? awardNote;

    // The one way a tender submission is recorded here — however it arrived (email, phone, post).
    // Saving goes through SaveExtractedQuote, so recipient/package statuses and
    // supersede-on-resubmit behave identically however the prices came in.
    private void OpenManualTenderModal()
    {
        if (busy || !CanEdit) return;
        showExtractModal = true;
        extractBusy = false;
        extractProposed = false;
        extractComplete = false;
        extractSourceEmail = null;
        extractIssues = new();
        extractSubcontractorNote = "";
        extractSubcontractorId = "";
        extractNotes = "";
        extractDrafts = lineItems
            .Select(item => new ExtractDraft { BidPackageLineItemId = item.LineItemId, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity })
            .ToList();
        if (extractDrafts.Count == 0) extractDrafts.Add(new ExtractDraft { Quantity = 1 });
    }


    private void CloseExtractModal() => showExtractModal = false;

    /// <summary>
    /// "Extract information" on a filed tender email: the AI reads the email (body + the returned
    /// pricing-schedule spreadsheet, extracted server-side) against the package's line schedule and
    /// pre-fills this modal with the submission it proposes plus every gap it found. The modal is
    /// the review step — the extraction saves NOTHING, and however the read goes the form falls
    /// back to the blank package schedule so the tender can always be keyed by hand.
    /// </summary>
    private async Task OpenExtractFromEmail(MailboxMessage email)
    {
        if (busy || extractBusy || !CanEdit) return;
        showExtractModal = true;
        extractBusy = true;
        extractProposed = false;
        extractComplete = false;
        extractSourceEmail = email;
        extractIssues = new();
        extractSubcontractorNote = "";
        extractSubcontractorId = "";
        extractNotes = "";
        extractDrafts = new();
        StateHasChanged();
        try
        {
            var proposal = await Commands.SendAsync(
                new ExtractTenderFromMessage(BidPackageId, email.Id), CancellationToken.None);
            extractProposed = proposal.Proposed;
            extractComplete = proposal.Complete;
            extractSubcontractorId = proposal.SubcontractorId ?? "";
            extractSubcontractorNote = proposal.SubcontractorNote;
            extractNotes = proposal.Notes;
            extractIssues = proposal.Issues.ToList();
            extractDrafts = proposal.Lines
                .Select(line => new ExtractDraft
                {
                    BidPackageLineItemId = line.BidPackageLineItemId,
                    Description = line.Description,
                    Unit = line.Unit,
                    Quantity = line.Quantity,
                    Rate = line.Rate,
                    Total = line.Total
                })
                .ToList();
        }
        catch (CommandFailedException ex)
        {
            extractIssues = new List<string> { ex.Message };
        }
        catch
        {
            extractIssues = new List<string> { "The tender couldn't be read just now — enter the submission manually, or close and try again." };
        }
        finally
        {
            if (extractDrafts.Count == 0)
                extractDrafts = lineItems
                    .Select(item => new ExtractDraft { BidPackageLineItemId = item.LineItemId, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity })
                    .ToList();
            if (extractDrafts.Count == 0) extractDrafts.Add(new ExtractDraft { Quantity = 1 });
            extractBusy = false;
            StateHasChanged();
        }
    }

    private void AddExtractLine() => extractDrafts.Add(new ExtractDraft { Quantity = 1 });

    private void RecalcTotal(ExtractDraft draft)
    {
        if (draft.Total == 0 && draft.Rate != 0 && draft.Quantity != 0)
            draft.Total = decimal.Round(draft.Rate * draft.Quantity, 2);
    }

    private async Task ConfirmSaveExtracted()
    {
        if (busy || !CanEdit || string.IsNullOrWhiteSpace(extractSubcontractorId)) return;
        error = null;
        try
        {
            busy = true;
            var lines = extractDrafts
                .Where(draft => !string.IsNullOrWhiteSpace(draft.Description))
                .Select(draft => new QuoteExtractionLine(
                    draft.BidPackageLineItemId, draft.Description.Trim(), (draft.Unit ?? "").Trim(),
                    draft.Quantity, draft.Rate, draft.Total))
                .ToList();
            await Commands.SendAsync(
                new SaveExtractedQuote(BidPackageId, extractSubcontractorId, extractNotes ?? "", lines), CancellationToken.None);
            showExtractModal = false;
            await LoadAsync();
        }
        catch { error = "Couldn't save that submission. Make sure a subcontractor is selected and every line has a description."; }
        finally { busy = false; }
    }

    // ---- Award summary & work-order email to the winner ----

    // The order this package's award raised (latest, if re-awarded). Null until orders load or
    // when the package has never been awarded.
    private ProjectWorkOrderDetail? AwardedOrder => projectOrders
        .Where(detail => string.Equals(detail.Order.BidPackageId, BidPackageId, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(detail => detail.Order.AwardedAt)
        .FirstOrDefault();

    private bool showWoEmailModal;
    private string woEmailSubject = "";
    private string woEmailBody = "";
    private string? woEmailNote;
    private string? woEmailLink;

    private void OpenWorkOrderEmailModal()
    {
        if (busy || package is null || AwardedOrder is not { } awarded) return;
        woEmailSubject = $"Work order WO-{awarded.Order.Number:0000} — {awarded.Order.Title} ({package.Reference})";
        woEmailBody = DefaultWorkOrderEmailBody(awarded);
        woEmailNote = null;
        showWoEmailModal = true;
    }

    private void CloseWorkOrderEmailModal() => showWoEmailModal = false;

    // The pre-filled order email: award confirmation, the priced lines (or the order total and scope
    // for legacy orders without lines), and the pre-start paperwork the tender invite asked about.
    private string DefaultWorkOrderEmailBody(ProjectWorkOrderDetail awarded)
    {
        var order = awarded.Order;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<p>Hello {awarded.SubcontractorName},</p>");
        sb.AppendLine($"<p>Following your tender for the <strong>{package!.Title}</strong> package (ref {package.Reference}), we are pleased to confirm the award and attach our work order <strong>WO-{order.Number:0000}</strong> below.</p>");
        if (awarded.Lines.Count > 0)
        {
            sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
            sb.AppendLine("<tr><th align=\"left\">Item</th><th align=\"left\">Qty</th><th align=\"left\">Unit</th><th align=\"right\">Total</th></tr>");
            foreach (var line in awarded.Lines.OrderBy(l => l.SortOrder))
                sb.AppendLine($"<tr><td>{line.Title}</td><td>{line.Quantity}</td><td>{line.Unit}</td><td align=\"right\">{line.LineTotal:£#,##0.00}</td></tr>");
            sb.AppendLine($"<tr><td colspan=\"3\"><strong>Order total</strong></td><td align=\"right\"><strong>{order.Value:£#,##0.00}</strong></td></tr>");
            sb.AppendLine("</table>");
        }
        else
        {
            sb.AppendLine($"<p><strong>Order value:</strong> {order.Value:£#,##0.00}</p>");
            if (!string.IsNullOrWhiteSpace(order.Scope))
                sb.AppendLine($"<p><strong>Scope:</strong> {order.Scope}</p>");
        }
        if (order.ScheduledCompletion is { } completion)
            sb.AppendLine($"<p><strong>Scheduled completion:</strong> {completion.LocalDateTime:d MMM yyyy}</p>");
        sb.AppendLine("<p>Please reply to confirm receipt and acceptance of this order, quoting the reference. Before starting on site, please provide your RAMS documentation and current insurance certificates as set out in the tender invitation.</p>");
        sb.AppendLine("<p>Kind regards,<br/>Jewel Bespoke Build</p>");
        return sb.ToString();
    }

    private async Task ConfirmWorkOrderEmailDraft()
    {
        if (busy || !CanEdit || AwardedOrder is not { } awarded) return;
        error = null;
        try
        {
            busy = true;
            var draft = await Commands.SendAsync(
                new PrepareWorkOrderEmailDraft(awarded.Order.WorkOrderId, woEmailSubject.Trim(), woEmailBody), CancellationToken.None);
            showWoEmailModal = false;
            woEmailLink = draft.WebLink;
            woEmailNote = $"Draft created in the shared mailbox to {draft.RecipientEmail}, tagged {package?.Reference}. Review and send it from the mailbox's Drafts folder.";
        }
        catch (CommandFailedException ex) { error = $"Couldn't create the draft: {ex.Message}"; }
        catch { error = "Couldn't create the draft. Check the supplier has an email address in the directory and the mailbox connection, then try again."; }
        finally { busy = false; }
    }

    // ---- Award: winning quote → work order (the purchase-order record) ----

    private async Task AwardTo(Quote quote)
    {
        if (busy || package is null || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            var sub = Subs.Find(quote.SubcontractorId);
            var workOrder = await Commands.SendAsync(
                new AwardBidPackage(
                    BidPackageId, ProjectId, quote.SubcontractorId, quote.Value,
                    $"{package.Title} ({package.Reference}) — as tender submission received {quote.ReceivedAt.LocalDateTime:d MMM yyyy}",
                    Auth.CurrentUser?.Email ?? "", quote.QuoteId), CancellationToken.None);
            awardNote = $"Awarded to {sub?.CompanyName ?? quote.SubcontractorId} at {quote.Value:£#,##0.00} — work order {workOrder.WorkOrderId[..8]}… raised as the purchase order.";
            await LoadAsync();
        }
        catch { error = "Couldn't award the package. Please try again."; }
        finally { busy = false; }
    }

    // ---- Line-item editing ----

    private sealed class LineDraft
    {
        public string Trade { get; set; } = "";
        public string Description { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Quantity { get; set; }
        public string CostCode { get; set; } = "";
    }

    // "00006-12 — Plastering" for a known code; the bare code when the master list hasn't loaded.
    private string CostCentreLabel(string code)
    {
        var centre = CostCenters.Alphabetical.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        return centre is null ? code : $"{centre.Code} — {centre.Name}";
    }

    // Flip the package's materials flag: when on, the drafted tender invite asks each
    // subcontractor to state whether they will supply their own materials or price labour-only.
    private async Task ToggleMaterialsApplicable(bool applicable)
    {
        if (busy || package is null || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            package = await Commands.SendAsync(
                new UpdateBidPackageScope(package.BidPackageId, package.Title, package.Trade, package.Status, package.OwnerEmail, applicable),
                CancellationToken.None);
        }
        catch { error = "Couldn't update the materials setting. Please try again."; }
        finally { busy = false; }
    }

    // ---- Close / reopen: the no-winner ending. Closing records ClosedAt and makes the page
    // read-only (CanEdit); reopening restores the status the package's data implies. ----

    private async Task ClosePackage()
    {
        if (busy || package is null || !CanManage
            || package.Status is BidPackageStatus.Awarded or BidPackageStatus.Closed) return;
        error = null;
        try
        {
            busy = true;
            package = await Commands.SendAsync(new CloseBidPackage(BidPackageId), CancellationToken.None);
        }
        catch { error = "Couldn't close the bid package. Please try again."; }
        finally { busy = false; }
    }

    private async Task ReopenPackage()
    {
        if (busy || package is null || !CanManage || package.Status != BidPackageStatus.Closed) return;
        error = null;
        try
        {
            busy = true;
            package = await Commands.SendAsync(new ReopenBidPackage(BidPackageId), CancellationToken.None);
        }
        catch { error = "Couldn't reopen the bid package. Please try again."; }
        finally { busy = false; }
    }

    // ---- Website links on the recipient list ----

    // Directory websites arrive as the search stored them — sometimes bare domains. A missing
    // scheme would make the browser treat the href as a relative path, so add one.
    private static string WebsiteHref(string website)
    {
        var trimmed = website.Trim();
        return trimmed.Contains("://") ? trimmed : $"https://{trimmed}";
    }

    // The label is the tidy form: no scheme, no www., no trailing slash — "fence-masters.co.uk".
    private static string WebsiteLabel(string website)
    {
        var label = website.Trim();
        var schemeAt = label.IndexOf("://", StringComparison.Ordinal);
        if (schemeAt >= 0) label = label[(schemeAt + 3)..];
        if (label.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) label = label[4..];
        return label.TrimEnd('/');
    }

    // ---- Delete: removes the record and its tender data for good. Guarded by a confirm modal;
    // the server refuses Awarded packages and anything a work order references. ----

    private bool showDeleteModal;

    private void OpenDeleteModal()
    {
        if (package is null || !CanManage || package.Status == BidPackageStatus.Awarded) return;
        showDeleteModal = true;
    }

    private void CloseDeleteModal()
    {
        if (busy) return;
        showDeleteModal = false;
    }

    private async Task ConfirmDelete()
    {
        if (busy || package is null || !CanManage || package.Status == BidPackageStatus.Awarded) return;
        error = null;
        try
        {
            busy = true;
            await Commands.SendAsync(new DeleteBidPackage(BidPackageId), CancellationToken.None);
            showDeleteModal = false;
            // Back to the register — this record no longer exists to stand on. busy stays true
            // so nothing is clickable during the navigation.
            Nav.NavigateTo($"/projects/{ProjectId}/bid-package-invites");
        }
        catch
        {
            error = "Couldn't delete the bid package — if it has a work order, cancel that first.";
            showDeleteModal = false;
            busy = false;
        }
    }

    // ---- Tender-document attachments: uploaded files that travel with the invite. ----

    private async Task OnAttachmentFilesSelected(InputFileChangeEventArgs e)
    {
        if (busy || !CanEdit) return;
        var files = e.GetMultipleFiles(20);
        if (files.Count == 0) return;
        error = null;
        try
        {
            busy = true;
            fetchedAttachments = await PackageAttachments.UploadFilesAsync(BidPackageId, files);
        }
        catch (Exception ex) { error = $"Couldn't upload: {ex.Message}"; }
        finally { busy = false; }
    }

    private async Task RemoveAttachment(BidPackageAttachment attachment)
    {
        if (busy || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            fetchedAttachments = await PackageAttachments.RemoveAsync(BidPackageId, attachment.BidPackageAttachmentId);
        }
        catch { error = "Couldn't remove the attachment. Please try again."; }
        finally { busy = false; }
    }

    private static string FormatFileSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024d * 1024d):0.#} MB",
        >= 1024 => $"{bytes / 1024d:0.#} KB",
        _ => $"{bytes} B"
    };

    // ---- Select line items from the valuation report (the fast path onto the package) ----
    //
    // The picker reads the LIVE report (ListValuationLinesForProject) — the same rows the
    // Valuation tab edits — so the cost codes on offer are exactly the ones with a sale-side
    // home, variations included. Nothing of the sale figures is persisted here: ticked rows
    // become plain BidPackageLineItemInputs and land through AddBidPackageLineItems, which
    // appends without touching existing lines' ids, coverage links or quote references.

    private bool showValuationPicker;
    private IReadOnlyList<ValuationLineItem>? valuationLines;   // null = no fetch has landed
    private bool valuationLinesFailed;
    private readonly HashSet<string> valuationSelection = new();
    private string valuationSearch = "";

    private void OnValuationSearchInput(ChangeEventArgs e) => valuationSearch = e.Value?.ToString() ?? "";

    // The search narrows what's LISTED, never what's SELECTED: ticks made under one search
    // survive the next, and the confirm reads valuationSelection against the full report.
    // Matches description (as the package line will carry it), cost code and section header,
    // so "SUB-PIL", "standing charge" and "PC sums" all narrow the way you'd expect.
    private IReadOnlyList<ValuationLineItem> FilteredValuationLines
    {
        get
        {
            var q = valuationSearch.Trim();
            if (q.Length == 0) return SelectableValuationLines;
            return SelectableValuationLines
                .Where(line => ValuationLineDescription(line).Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (line.CostCode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || ValuationGroupLabel(line).Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    // Declined rows are recorded on the report but priced into nothing — not scope to tender.
    // Order mirrors the report: element blocks in bill order, DisplayOrder within.
    private IReadOnlyList<ValuationLineItem> SelectableValuationLines => (valuationLines ?? Array.Empty<ValuationLineItem>())
        .Where(line => line.LineType != ValuationLineType.Declined)
        .OrderBy(line => line.ElementType)
        .ThenBy(line => line.DisplayOrder)
        .ToList();

    private async Task OpenValuationPicker()
    {
        if (busy || package is null || !CanEdit) return;
        valuationSelection.Clear();
        valuationSearch = "";
        error = null;
        showValuationPicker = true;
        // Stale-while-revalidate: rows already fetched keep showing while the fresh fetch lands,
        // so reopening the picker after editing the report picks up the change.
        try
        {
            valuationLines = await Queries.AskAsync(new ListValuationLinesForProject(ProjectId), CancellationToken.None);
            valuationLinesFailed = false;
        }
        catch { valuationLinesFailed = valuationLines is null; }
    }

    private void CloseValuationPicker()
    {
        if (busy) return;
        showValuationPicker = false;
        valuationSelection.Clear();
        valuationSearch = "";
    }

    private void ToggleValuationLine(ValuationLineItem line)
    {
        if (string.IsNullOrWhiteSpace(line.CostCode)) return;
        if (!valuationSelection.Remove(line.ValuationLineItemId)) valuationSelection.Add(line.ValuationLineItemId);
    }

    private void ToggleValuationGroup(IEnumerable<ValuationLineItem> group)
    {
        var selectable = group.Where(l => !string.IsNullOrWhiteSpace(l.CostCode)).Select(l => l.ValuationLineItemId).ToList();
        if (selectable.Count == 0) return;
        if (selectable.All(valuationSelection.Contains)) valuationSelection.ExceptWith(selectable);
        else valuationSelection.UnionWith(selectable);
    }

    private async Task ConfirmValuationPicker()
    {
        if (busy || package is null || !CanEdit || valuationSelection.Count == 0) return;
        error = null;
        try
        {
            busy = true;
            var inputs = SelectableValuationLines
                .Where(line => valuationSelection.Contains(line.ValuationLineItemId) && !string.IsNullOrWhiteSpace(line.CostCode))
                .Select(line => new BidPackageLineItemInput(
                    ValuationLineDescription(line),
                    line.Unit.Trim(),
                    line.Quantity,
                    package.Trade,
                    line.CostCode.Trim()))
                .ToList();
            fetchedLineItems = await Commands.SendAsync(new AddBidPackageLineItems(BidPackageId, inputs), CancellationToken.None);
            showValuationPicker = false;
            valuationSelection.Clear();
        }
        catch { error = "Couldn't add the selected line items — check the package's line list before retrying."; }
        finally { busy = false; }
    }

    // Section headers mirroring the report's blocks; contract works keep their section identity.
    private static string ValuationGroupLabel(ValuationLineItem line) => line.ElementType switch
    {
        ValuationElementType.ContractWorks =>
            string.IsNullOrWhiteSpace(line.SectionCode) && string.IsNullOrWhiteSpace(line.SectionName)
                ? "Contract works"
                : $"{line.SectionCode} — {line.SectionName}".Trim(' ', '—'),
        ValuationElementType.PcSum => "PC sums",
        ValuationElementType.Contingency => "Contingency",
        _ => "Variations",
    };

    // The description the package line will carry. Variation lines lead with their V-number so
    // the tenderer's schedule says which change the scope belongs to; blank descriptions fall
    // back to the variation title or the section name rather than arriving empty.
    private static string ValuationLineDescription(ValuationLineItem line)
    {
        var description = line.Description.Trim();
        if (line.ElementType == ValuationElementType.Variation)
        {
            if (description.Length == 0) description = line.VariationTitle.Trim();
            var reference = line.VariationRef.Trim();
            if (reference.Length > 0 && description.Length > 0) description = $"{reference} — {description}";
            else if (reference.Length > 0) description = reference;
        }
        return description.Length == 0 ? line.SectionName.Trim() : description;
    }

    private static string? ValuationLineTypeBadge(ValuationLineItem line) => line.LineType switch
    {
        ValuationLineType.ProvisionalSum => "PS",
        ValuationLineType.Omit => "Omit",
        ValuationLineType.Tbc => "TBC",
        _ => null,
    };

    // ---- Package details: the specification summary + the line-item schedule, edited
    // together in ONE dialog. One act of authorship, one save — and the one shape the AI flow
    // can fill in a single update (splitting them across two dialogs relied on the model
    // following through across turns, and it didn't: 2026-08-16). ----

    private bool showDetailsModal;
    private string specDraft = "";
    private List<LineDraft> lineDrafts = new();

    private void EditDetails()
    {
        if (!CanEdit || package is null) return;
        specDraft = package.SpecificationSummary;
        lineDrafts = lineItems
            .Select(item => new LineDraft { Trade = item.Trade, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity, CostCode = item.CostCode })
            .ToList();
        if (lineDrafts.Count == 0) lineDrafts.Add(new LineDraft());
        error = null;
        showDetailsModal = true;
    }

    private void AddLine()
    {
        lineDrafts.Add(new LineDraft());
    }

    private void RemoveLine(LineDraft draft)
    {
        lineDrafts.Remove(draft);
    }

    private void CancelDetails()
    {
        if (busy) return;
        showDetailsModal = false;
        lineDrafts.Clear();
    }

    private async Task SaveDetails()
    {
        if (busy || package is null || !CanEdit) return;
        error = null;
        try
        {
            var kept = lineDrafts
                .Where(draft => !string.IsNullOrWhiteSpace(draft.Description))
                .ToList();
            // Every line put out to tender must know its cost-centre home.
            if (kept.Any(draft => string.IsNullOrWhiteSpace(draft.CostCode)))
            {
                error = "Every line item needs a cost code — pick a cost centre for each line before saving.";
                return;
            }
            busy = true;

            // Summary first, then the schedule — two commands behind one Save. If the second
            // fails the first has still landed; the catch says so and the dialog stays open with
            // everything the user typed.
            package = await Commands.SendAsync(
                new UpdateBidPackageScope(package.BidPackageId, package.Title, package.Trade, package.Status,
                    package.OwnerEmail, package.MaterialsApplicable, specDraft.Trim()),
                CancellationToken.None);

            var inputs = kept
                .Select(draft => new BidPackageLineItemInput(
                    draft.Description.Trim(),
                    (draft.Unit ?? "").Trim(),
                    draft.Quantity,
                    (draft.Trade ?? "").Trim(),
                    draft.CostCode.Trim()))
                .ToList();
            fetchedLineItems = await Commands.SendAsync(new SetBidPackageLineItems(BidPackageId, inputs), CancellationToken.None);

            showDetailsModal = false;
            lineDrafts.Clear();
        }
        catch { error = "Couldn't save the package details — check what's on the record before retrying, the summary may have saved without the lines."; }
        finally { busy = false; }
    }
}
