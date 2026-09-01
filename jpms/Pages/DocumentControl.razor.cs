using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;


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
    private string? fileNote;

    // ---- The source email: fetched live per item on first open; null after a failed fetch,
    //      which the pane renders as the snapshot-only fallback. ----
    private bool sourceEmailOpen;
    private bool sourceEmailLoading;
    private MailboxMessageDetail? sourceEmail;

}
