

namespace Jewel.JPMS.Pages;

public partial class DocumentControl
{
    private enum DocView { Queue, Filed, Discarded }
    private enum FileDestination { Drawing, PaymentCertificate, Subcontractor }

    // Suggested kinds — the three Document Triage groupings first (RAMS, insurance,
    // drawings/specs), then the portal upload's own insurance spellings so a filing here lands
    // in the same version history as a portal upload of the same document.
    private static readonly string[] SubcontractorDocumentKinds =
    {
        "RAMS", "Insurance", "Drawings / Specifications",
        "Public liability insurance", "Employers liability insurance"
    };


    // Nullable backing field: null is the honest "not fetched yet" (the section renders a gate,
    // never a confident empty state, until the fetch lands or fails).
    private IReadOnlyList<DocumentControlItem>? items;
    private string? loadError;

    private DocView view = DocView.Queue;
    private string? selectedId;
    private bool busy;
    private string busyLabel = "Working";
    private string? actionError;
    // The last completed action's outcome ("Filed as…", "Extracted 3 files…"). Every action
    // CLOSES the open document (2026-09-03 rule: once it's done, the document leaves the screen
    // and the page waits for the next pick), so this renders in the right pane's empty state —
    // not beside a document that has already left the list — until the next selection or view
    // switch.
    private string? doneNote;
    // Queue items that appeared from the last archive extraction: badged and tinted in the list
    // until each is opened, so the unpacked files are findable among the rest of the queue.
    private readonly HashSet<string> freshIds = new(StringComparer.Ordinal);

    // ---- The source email: fetched live per item on first open; null after a failed fetch,
    //      which the pane renders as the snapshot-only fallback. ----
    private bool sourceEmailOpen;
    private bool sourceEmailLoading;
    private MailboxMessageDetail? sourceEmail;

}
