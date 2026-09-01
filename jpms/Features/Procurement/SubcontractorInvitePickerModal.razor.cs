using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Features.Procurement;

/// <summary>What the picker hands back: the ticked directory companies, and the quick-add
/// contact when its row was completed (name, valid email, and a trade when one was chosen).</summary>
public sealed record SubcontractorInvitePick(IReadOnlyList<string> SubcontractorIds, QuickAddContact? QuickAdd);

/// <summary>An ad-hoc invitee typed straight into the picker. TradeId may be empty — the host
/// falls back to the package's own trade when saving the prospect.</summary>
public sealed record QuickAddContact(string Name, string Email, string TradeId);

public partial class SubcontractorInvitePickerModal
{
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Already on the tender list — never offered again.</summary>
    [Parameter] public IReadOnlyCollection<string> InvitedIds { get; set; } = Array.Empty<string>();

    /// <summary>The package's trade: pre-filters the list when the directory knows it, and
    /// pre-fills the quick-add trade when the host resolves an id for it.</summary>
    [Parameter] public string DefaultTrade { get; set; } = "";
    [Parameter] public string DefaultTradeId { get; set; } = "";

    /// <summary>The host's in-flight flag while the confirm's commands run.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<SubcontractorInvitePick> OnConfirm { get; set; }

    private string search = "";
    private string? tradeFilter;
    private readonly HashSet<string> selection = new(StringComparer.OrdinalIgnoreCase);
    private string quickAddName = "";
    private string quickAddEmail = "";
    private string quickAddTradeId = "";
    private bool wasOpen;
    private bool confirming;

    /// <summary>Each opening starts clean: ticks and search cleared, the trade filter and the
    /// quick-add trade pre-set from the package — one less tap for the normal case. A close that
    /// follows a confirm also clears the quick-add row (it was saved); a cancel keeps it, so an
    /// accidental close doesn't lose a typed contact.</summary>
    protected override void OnParametersSet()
    {
        if (IsOpen == wasOpen) return;
        wasOpen = IsOpen;
        if (!IsOpen)
        {
            if (confirming) { quickAddName = quickAddEmail = quickAddTradeId = ""; }
            confirming = false;
            return;
        }
        selection.Clear();
        search = "";
        if (string.IsNullOrWhiteSpace(quickAddTradeId)) quickAddTradeId = DefaultTradeId;
        tradeFilter = InvitableTrades
            .FirstOrDefault(t => string.Equals(t, DefaultTrade, StringComparison.OrdinalIgnoreCase));
    }

    // The distinct trade names among companies that could be invited (never clients/architects,
    // never tender-only prospects — the picker is the curated directory).
    // A company with several trades appears under each of them.
    private IReadOnlyList<string> InvitableTrades =>
        Subs.All()
            .Where(s => !s.IsProspect)
            .Where(s => s.Category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier)
            .SelectMany(s => s.Trades)
            .Select(t => t.Name.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private IReadOnlyList<Subcontractor> Invitable
    {
        get
        {
            var invited = InvitedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var q = search.Trim();
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
    }

    private void Toggle(string subcontractorId, ChangeEventArgs e)
    {
        if (e.Value is true) selection.Add(subcontractorId);
        else selection.Remove(subcontractorId);
    }

    private bool QuickAddReady =>
        !string.IsNullOrWhiteSpace(quickAddName)
        && quickAddEmail.Contains('@') && quickAddEmail.Trim().Length >= 5;

    // Something typed but not enough to include — surfaced so a half-filled row isn't silently dropped.
    private bool QuickAddPartial =>
        !QuickAddReady && (!string.IsNullOrWhiteSpace(quickAddName) || !string.IsNullOrWhiteSpace(quickAddEmail));

    private int PickCount => selection.Count + (QuickAddReady ? 1 : 0);

    // The host saves and invites; a failure keeps the modal open with everything still ticked.
    private async Task ConfirmAsync()
    {
        if (Busy || PickCount == 0) return;
        confirming = true;
        var pick = new SubcontractorInvitePick(
            selection.ToList(),
            QuickAddReady ? new QuickAddContact(quickAddName.Trim(), quickAddEmail.Trim(), quickAddTradeId) : null);
        await OnConfirm.InvokeAsync(pick);
        // Still open once the host is done ⇒ the save failed: the next close is a cancel.
        if (wasOpen) confirming = false;
    }
}
