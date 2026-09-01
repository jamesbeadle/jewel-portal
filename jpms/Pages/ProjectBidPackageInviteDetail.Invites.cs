using System.Text.Json;
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

}
