namespace Jewel.JPMS.Models;

// The "RF" family plus the JCT time/notice instruments they trigger.
// Explicit integer values are pinned so existing stored rows keep their meaning:
//   legacy Submittal(1) -> Rfa, legacy Variation(2) -> Rfc, NoticeOfDelay stays 3.
public enum RequestType
{
    Rfi = 0,            // Request for Information
    Rfa = 1,            // Request for Approval (sample / submittal)
    Rfc = 2,            // Request for Change / Comment
    NoticeOfDelay = 3,  // NOD (JCT ICD 2024 cl. 2.19)
    Rfq = 4,            // Request for Quotation
    Rfp = 5,            // Request for Proposal
    ExtensionOfTime = 6,// EOT (JCT ICD 2024 cl. 2.19/2.20)
    General = 7         // Default state: project-tagged & cost centre known, not yet promoted
}

public enum RequestStatus
{
    Open = 0,
    AwaitingResponse = 1,
    Approved = 2,
    Rejected = 3,
    Closed = 4,
    Responded = 5
}

public sealed record Request(
    string RequestId,
    string ProjectId,
    RequestType Kind,
    string Reference,
    string Title,
    string Description,
    RequestStatus Status,
    decimal? Value,
    string RaisedByEmail,
    DateTimeOffset RaisedAt,
    DateTimeOffset? RespondedAt,
    string? ResponseText = null,
    string? RespondedByEmail = null,
    bool ImpliesVariation = false,
    string? RaisedTo = null,            // ball-in-court party (e.g. PLG Architects)
    string? DrawingRef = null,          // drawing / detail reference the request concerns
    DateTimeOffset? ResponseDue = null, // contractual response-due date
    string? RelatedDrawingSpec = null,  // related drawing / spec issued with the response
    string? InternalNotes = null,       // notes kept internal to Jewel
    string? ClientNotes = null,         // notes shared with client / external parties
    int Number = 0,                     // sequential request number; rendered as REQ-0001
    bool HasRfq = false,                // an RFI that has spawned an RFQ (unlocks VOQ creation)
    PartyKind PartyKind = PartyKind.Client, // what kind of party PartyId points at (client or architect)
    string? PartyId = null,             // the party corresponded with — recipient source on RFI promotion
    string? OnBehalfOfClientId = null,  // when the party is an architect: the client they act for (optional)
    string? BasisOfQueries = null,          // official document: what the queries arise from (emails, drawings, site observations)
    string? ResponseActionRequired = null,  // official document: the written confirmation / instruction being asked for
    string? ImpactIfLate = null,            // official document: consequence if no response by the required-by date
    IReadOnlyList<RequestItem>? Items = null, // official document: the itemised queries, ordered by Position
    string? RelatedNodRequestId = null,     // EOT only: the Notice of Delay this EOT arises from (optional)
    string? MergedIntoRequestId = null,     // set when this General request was merged into another (the survivor's id)
    DateTimeOffset? ClosedAt = null,        // when the request was closed — user-chosen (today or prior), cleared on reopen
    DateTimeOffset? IssuedAt = null,        // when the official document was issued — user-set/updated, never stamped automatically
    string? RaisedToContactId = null,       // the project contact RaisedTo points at, when picked from the project's contact list (RaisedTo stays the denormalised display string)
    bool CriticalPath = false)              // Critical Path tag — the RFI is programme-related; shows in the Programme tab's "Critical Path RFIs" view
{
    // Human-readable request number / mailbox folder name (e.g. "REQ-0001"). Empty until assigned.
    public string DisplayNumber => Number > 0 ? $"REQ-{Number:0000}" : "";

    // The itemised queries, never null (Items is nullable so old payloads deserialize cleanly).
    public IReadOnlyList<RequestItem> ItemList => Items ?? Array.Empty<RequestItem>();

    // Working days a still-open request has been outstanding since it was issued.
    public int? DaysOutstanding =>
        Status is RequestStatus.Closed || RespondedAt is not null
            ? null
            : Math.Max(0, (int)(DateTimeOffset.UtcNow.Date - RaisedAt.Date).TotalDays);

    // Open and older than 7 days without a response.
    public bool IsOverdue => DaysOutstanding is > 7;
}

/// <summary>
/// One itemised query on a request's official document — a numbered row of the RFI sheet
/// (Item / Drawing Ref / Member-Area / Query / Response). The rendered item number is the 1-based
/// <paramref name="Position"/>.
/// </summary>
public sealed record RequestItem(
    string RequestItemId,
    string RequestId,
    int Position,
    string DrawingRef,
    string MemberArea,
    string Query,
    string? Response = null);

public static class RequestTypeExtensions
{
    // EMAIL POLICY — which request kinds may ever produce an email draft of their official
    // document. Only the official instruments are emailed: the RFI and the JCT time notices
    // (NOD / EOT). A General container, RFA, RFC, RFQ and RFP are NEVER emailed: an RFQ reaches
    // subcontractors as a bid-package invite (its own draft flow), and VOQ / VO financial
    // documents are not request emails. Server handlers and UI both consult this single gate.
    public static bool IsEmailable(this RequestType kind) =>
        kind is RequestType.Rfi or RequestType.NoticeOfDelay or RequestType.ExtensionOfTime;

    public static string DisplayName(this RequestType kind) => kind switch
    {
        RequestType.Rfi             => "RFI",
        RequestType.Rfq             => "RFQ",
        RequestType.Rfp             => "RFP",
        RequestType.Rfc             => "RFC",
        RequestType.Rfa             => "RFA",
        RequestType.NoticeOfDelay   => "NOD",
        RequestType.ExtensionOfTime => "EOT",
        RequestType.General         => "General",
        _ => kind.ToString()
    };

    public static string LongName(this RequestType kind) => kind switch
    {
        RequestType.Rfi             => "Request for Information",
        RequestType.Rfq             => "Request for Quotation",
        RequestType.Rfp             => "Request for Proposal",
        RequestType.Rfc             => "Request for Change",
        RequestType.Rfa             => "Request for Approval",
        RequestType.NoticeOfDelay   => "Notice of Delay",
        RequestType.ExtensionOfTime => "Extension of Time",
        RequestType.General         => "General Request",
        _ => kind.ToString()
    };
}
