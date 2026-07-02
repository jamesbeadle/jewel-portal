using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Documents;

/// <summary>
/// Everything needed to render a request (RFI/RFA/RFC/…) document, collated from the SQL source of
/// truth. The model is a flat, self-contained snapshot so the renderer has no database dependency and
/// the same shape can be produced for the download endpoint (api) and the outbound send (worker).
///
/// The document is regenerated from SQL every time — on creation, on download, and on every resend —
/// so it is always idempotent: the bytes are a pure function of the current request state, nothing is
/// stored. Two renders of an unchanged request differ only by <see cref="GeneratedAt"/>.
/// </summary>
public sealed record RequestDocumentModel(
    string RequestId,
    string DisplayNumber,     // REQ-0001
    string TypeShort,         // RFI
    string TypeLong,          // Request for Information
    string Title,
    string Description,
    string StatusLabel,
    string ProjectName,
    string ProjectReference,
    string ClientName,
    string RaisedByEmail,
    DateTimeOffset RaisedAt,
    DateTimeOffset? ResponseDue,
    string? RaisedTo,             // ball-in-court / responding party
    string? DrawingRef,
    string? RelatedDrawingSpec,
    decimal? Value,
    bool ImpliesVariation,
    string? ClientNotes,
    string? ResponseText,
    string? RespondedByEmail,
    DateTimeOffset? RespondedAt,
    IReadOnlyList<RequestDocumentRecipient> Recipients,
    IReadOnlyList<RequestDocumentActivity> Activity,
    DateTimeOffset GeneratedAt,
    // ---- Official document (RFI sheet) body: all optional, rendered only when present ----------
    string Reference = "",                            // client-visible reference, e.g. RFI-052
    string? BasisOfQueries = null,                    // what the queries arise from
    string? ResponseActionRequired = null,            // the confirmation / instruction requested
    string? ImpactIfLate = null,                      // consequence of missing the required-by date
    IReadOnlyList<RequestDocumentItem>? Items = null) // the itemised queries, in order
{
    /// <summary>The itemised queries, never null (Items is nullable so older call sites stay valid).</summary>
    public IReadOnlyList<RequestDocumentItem> ItemList => Items ?? Array.Empty<RequestDocumentItem>();

    /// <summary>The client-facing reference: the project-local number (RFI-052) when one has been
    /// minted, otherwise the internal REQ number.</summary>
    public string DisplayReference => string.IsNullOrWhiteSpace(Reference) ? DisplayNumber : Reference;

    /// <summary>True when a still-open request has been outstanding past its response-due date.</summary>
    public bool IsOverdue =>
        RespondedAt is null
        && ResponseDue is { } due
        && DateTimeOffset.UtcNow > due;

    /// <summary>A safe, human file name for the PDF — the client-visible reference plus the subject,
    /// e.g. "RFI-052 - Shower Trays.pdf" (falling back to "REQ-0001 - RFI.pdf" pre-numbering).</summary>
    public string FileName
    {
        get
        {
            var title = Title.Trim();
            if (title.Length > 60) title = title[..60].TrimEnd();
            var stem = !string.IsNullOrWhiteSpace(Reference)
                ? (title.Length > 0 ? $"{Reference} - {title}" : Reference)
                : string.IsNullOrEmpty(DisplayNumber) ? TypeShort : $"{DisplayNumber} - {TypeShort}";
            foreach (var c in Path.GetInvalidFileNameChars())
                stem = stem.Replace(c, '-');
            return stem + ".pdf";
        }
    }

    /// <summary>The email subject line used when the document is sent, resent or drafted.</summary>
    public string EmailSubject =>
        !string.IsNullOrWhiteSpace(Reference)
            ? $"{Reference}: {Title} — {ProjectName}"
            : string.IsNullOrEmpty(DisplayNumber)
                ? $"{TypeShort}: {Title} — {ProjectName}"
                : $"{DisplayNumber} {TypeShort}: {Title} — {ProjectName}";
}

/// <summary>One numbered row of the itemised-queries table (Item / Drawing Ref / Member-Area / Query / Response).</summary>
public sealed record RequestDocumentItem(
    int Position,
    string DrawingRef,
    string MemberArea,
    string Query,
    string? Response);

/// <summary>An external party the document is issued to (a project contact flagged ReceivesRequests).</summary>
public sealed record RequestDocumentRecipient(string Name, string Email, string Role, string? Organisation);

/// <summary>One entry in the request's shared activity history, rendered as the audit trail.</summary>
public sealed record RequestDocumentActivity(string AuthorName, string Body, DateTimeOffset PostedAt, bool Inbound);
