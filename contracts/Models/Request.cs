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
    ExtensionOfTime = 6 // EOT (JCT ICD 2024 cl. 2.19/2.20)
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
    int Number = 0)                     // sequential request number; rendered as REQ-0001
{
    // Human-readable request number / mailbox folder name (e.g. "REQ-0001"). Empty until assigned.
    public string DisplayNumber => Number > 0 ? $"REQ-{Number:0000}" : "";

    // Working days a still-open request has been outstanding since it was issued.
    public int? DaysOutstanding =>
        Status is RequestStatus.Closed || RespondedAt is not null
            ? null
            : Math.Max(0, (int)(DateTimeOffset.UtcNow.Date - RaisedAt.Date).TotalDays);

    // Open and older than 7 days without a response.
    public bool IsOverdue => DaysOutstanding is > 7;
}

public static class RequestTypeExtensions
{
    public static string DisplayName(this RequestType kind) => kind switch
    {
        RequestType.Rfi             => "RFI",
        RequestType.Rfq             => "RFQ",
        RequestType.Rfp             => "RFP",
        RequestType.Rfc             => "RFC",
        RequestType.Rfa             => "RFA",
        RequestType.NoticeOfDelay   => "NOD",
        RequestType.ExtensionOfTime => "EOT",
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
        _ => kind.ToString()
    };
}
