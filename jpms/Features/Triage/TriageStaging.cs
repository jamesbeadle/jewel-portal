namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// One drafted to-do row in the triage to-dos modal. The ASSIGNEES are held as
/// TodoAssigneePicker values — a role, optionally pinned to a named holder. Empty = unassigned.
/// A row with several assignees is raised as one to-do PER ASSIGNEE — same title, detail and due
/// date, separate TODO-#### references and separate tick-boxes — so an email that needs two
/// people to act becomes two items in one apply.
/// </summary>
public sealed class TodoDraftRow
{
    public string Title { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<string> Assignees { get; } = new();
    // New drafts start due one week out — the house default for an item raised today. The field
    // stays editable (or clearable) in the modal; this is a starting value, not a rule.
    public string Due { get; set; } = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
}

/// <summary>
/// A NEW system record staged in the System Tags modal, created (and the email tagged to it) when
/// the page's Apply runs. Which fields matter depends on <see cref="Kind"/>: a Client-side General
/// request carries the request fields; a Subcontractor bid package carries Title + Trade; a
/// Subcontractor work order carries the full manual-order surface (subcontractor, scope,
/// programme, priced lines, deposit, draft flag); a Subcontractor defect carries location,
/// description and assigned-to.
/// </summary>
public sealed class StagedRecordCreate
{
    public StagedRecordKind Kind { get; set; } = StagedRecordKind.Request;

    // Which request the Request kind raises: General ("Raise Request") or an official RFI
    // ("Raise RFI") — the server mints the matching reference either way (REQ-#### global, or
    // the project's own RFI sequence).
    public Jewel.JPMS.Models.RequestType RequestKind { get; set; }
        = Jewel.JPMS.Models.RequestType.General;

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DrawingRef { get; set; } = "";
    public string ResponseDue { get; set; } = "";
    public bool AddToProgramme { get; set; }
    public string Trade { get; set; } = "";

    // ---- Work order fields (Kind == WorkOrder) — mirroring ManualWorkOrderModal's surface. ----
    public string SubcontractorId { get; set; } = "";
    public string Scope { get; set; } = "";
    // Date-only strings ("yyyy-MM-dd", like ResponseDue above); blank = not set.
    public string ProgrammeStart { get; set; } = "";
    public string TargetCompletion { get; set; } = "";
    public string ProgrammeNotes { get; set; } = "";
    public bool DepositRequired { get; set; }
    // Parsed like the line amounts (invariant decimal text).
    public string DepositPercentText { get; set; } = "";
    // Store the order as an unnumbered draft rather than releasing it on apply.
    public bool SaveAsDraft { get; set; }
    public List<StagedWorkOrderLine> Lines { get; } = new() { new StagedWorkOrderLine() };

    // ---- Attachments for the staged work order — record keeping only, never sent to the
    //      supplier (the PO email and printed PO ignore them). ----
    // Graph attachment ids ticked from the open email; the server copies the bytes mailbox →
    // blob store when the apply raises the order, so they never round-trip through the browser.
    public List<string> EmailAttachmentIds { get; } = new();
    // Files picked from this computer; the page uploads them onto the new order right after the
    // apply raises it (multipart, same transport as request attachments).
    public List<Microsoft.AspNetCore.Components.Forms.IBrowserFile> UploadFiles { get; } = new();

    // ---- Defect fields (Kind == Defect) — mirroring RaiseDefect's surface. Description is shared
    // with the request form above (both are "what's wrong" prose). ----
    public string DefectLocation { get; set; } = "";
    // Who the remediation is chased with — pre-filled from the email's sender, freely editable.
    public string DefectAssignedTo { get; set; } = "";

    public string Label => Kind switch
    {
        StagedRecordKind.BidPackage => "new bid package",
        StagedRecordKind.WorkOrder => "new work order",
        StagedRecordKind.Defect => "new defect",
        _ => RequestKind == Jewel.JPMS.Models.RequestType.Rfi ? "new RFI" : "new request"
    };

    /// <summary>What the staged-record chip shows after the label: the title — or for a defect,
    /// which has no title, its location (else the start of its description).</summary>
    public string DisplayTitle => Kind == StagedRecordKind.Defect
        ? (!string.IsNullOrWhiteSpace(DefectLocation)
            ? DefectLocation
            : Description.Length > 48 ? Description[..48] + "…" : Description)
        : Title;

    /// <summary>Lines the apply will actually send: rows where anything has been entered.</summary>
    public IEnumerable<StagedWorkOrderLine> EnteredLines =>
        Lines.Where(line => line.CostCode != ""
                            || !string.IsNullOrWhiteSpace(line.Title)
                            || !string.IsNullOrWhiteSpace(line.AmountText));

    /// <summary>
    /// What still stops the staged work order being raised — null when it is complete. Shared by
    /// the modal (inline hint) and the page's Apply (hard gate), so the wording is decided once.
    /// Mirrors ManualWorkOrderModal's client-side validation and the server's own rules.
    /// </summary>
    public string? WorkOrderProblem
    {
        get
        {
            if (Kind != StagedRecordKind.WorkOrder) return null;
            if (string.IsNullOrWhiteSpace(Title)) return "Give the work order a title.";
            if (string.IsNullOrWhiteSpace(SubcontractorId)) return "Choose the subcontractor the order is raised to.";
            var entered = EnteredLines.ToList();
            if (entered.Count == 0) return "Add at least one priced line.";
            if (entered.Any(line => line.CostCode == "")) return "Choose a cost centre for every line.";
            if (entered.Any(line => string.IsNullOrWhiteSpace(line.Title))) return "Every line needs a title.";
            if (entered.Any(line => line.Amount is not { } amount || amount == 0m))
                return "Every line needs a non-zero amount.";
            if (DepositRequired && (ParseDecimal(DepositPercentText) is not { } percent || percent <= 0m || percent > 100m))
                return "A required deposit needs a percentage above 0 and no more than 100.";
            return null;
        }
    }

    /// <summary>
    /// What still stops the staged defect being raised — null when it is complete. Shared by the
    /// modal (inline hint) and the page's Apply (hard gate), so the wording is decided once.
    /// Mirrors the server's own rule (RaiseDefectValidation: a description is required).
    /// </summary>
    public string? DefectProblem
    {
        get
        {
            if (Kind != StagedRecordKind.Defect) return null;
            if (string.IsNullOrWhiteSpace(Description)) return "Describe the defect.";
            return null;
        }
    }

    public static decimal? ParseDecimal(string text) =>
        decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
}

/// <summary>One staged priced line: its cost centre, what it covers, its £ amount as typed, and
/// the optional longer detail printed on the purchase order — same shape as ManualWorkOrderLine,
/// held as text until apply so half-typed amounts never throw.</summary>
public sealed class StagedWorkOrderLine
{
    public string CostCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string AmountText { get; set; } = "";
    public string Description { get; set; } = "";

    public decimal? Amount => StagedRecordCreate.ParseDecimal(AmountText);
}

public enum StagedRecordKind { Request, BidPackage, WorkOrder, Defect }

/// <summary>
/// A reply LINED UP IN THE OUTBOX: written against an older email read in the Control Centre
/// (a record's correspondence, the subcontractor comms browser) and sent when the page's Apply
/// runs. The apply first tags the anchor email to the records picked in System Tags for this
/// triage — one triage decision covers the open email and every email being answered — then sends
/// the reply, whose sent copy self-files by inheriting the anchor's tags (new ones included).
/// The anchor fields are a snapshot for the Outbox card; the envelope is editable until Apply.
/// </summary>
public sealed class StagedOutboxReply
{
    // ---- The anchor: which email this reply answers, as the Outbox card shows it. ----
    public required string MessageId { get; init; }
    public string? InternetMessageId { get; init; }
    public string AnchorSubject { get; init; } = "";
    public string AnchorFrom { get; init; } = "";
    public DateTimeOffset AnchorReceivedAt { get; init; }
    /// <summary>The workflow tags the anchor already carries (its record chips on the card) —
    /// display only; Apply reads the live message, not this snapshot.</summary>
    public IReadOnlyList<string> AnchorTags { get; init; } = Array.Empty<string>();

    // ---- The envelope, exactly as the composer showed it (semicolon/comma-separated fields). ----
    public string ToField { get; set; } = "";
    public string CcField { get; set; } = "";
    public string BccField { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public IReadOnlyList<ComposeDraftAttachment> Attachments { get; set; } = Array.Empty<ComposeDraftAttachment>();

    /// <summary>
    /// What still stops this reply being sent — null when it is complete. Shared by the composer
    /// (inline hint) and the page's Apply (hard gate), so the wording is decided once — the same
    /// "decision not yet made" rule as the staged work order and defect.
    /// </summary>
    public string? Problem
    {
        get
        {
            if (MailCompose.ParseRecipients(ToField).Count == 0) return "Add a To recipient.";
            if (string.IsNullOrWhiteSpace(Subject)) return "Write a subject.";
            if (!MailCompose.HtmlHasContent(Body)) return "Write the reply.";
            return null;
        }
    }
}
