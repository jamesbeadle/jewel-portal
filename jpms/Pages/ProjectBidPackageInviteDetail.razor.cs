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

}
