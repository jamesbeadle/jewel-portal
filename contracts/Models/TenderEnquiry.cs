namespace Jewel.JPMS.Models;

/// <summary>
/// An inbound tender enquiry: an architect (or client) inviting Jewel to tender for a job. Lives on
/// a project at the Lead stage — logging the enquiry from its email creates that project shell, so
/// the drawings, correspondence and document control it attracts have a home from day one. The
/// enquiry tracks the client-side journey (PQQ → shortlist → tender → decision); its emails are
/// read live by the "JPMS/TEQ-####" tag like every other record.
/// </summary>
public sealed record TenderEnquiry(
    string TenderEnquiryId,
    string ProjectId,
    int Number,                          // global sequence behind the TEQ-#### reference
    string Title,                        // the job as the architect names it — usually the site address
    string ArchitectPracticeName,
    string ArchitectContactName,
    string ArchitectContactEmail,
    string ScopeSummary,                 // what the works principally comprise
    string ContractForm,                 // e.g. "JCT Intermediate with Contractor's Design"
    TenderEnquiryStatus Status,
    DateTimeOffset ReceivedAt,           // the official date — when the invitation arrived (the "Issued" analogue)
    DateTimeOffset? PqqDueAt,            // when the questionnaire must be returned
    DateTimeOffset? TenderDueAt,         // when the priced tender must be returned, once known
    DateTimeOffset? PqqSubmittedAt,
    DateTimeOffset? TenderSubmittedAt,
    DateTimeOffset? DecidedAt,           // stamped on an ending status (declined, not shortlisted, won, lost)
    string DecisionNote,                 // why it ended that way, when someone said
    string OwnerEmail,                   // who is running the bid
    DateTimeOffset CreatedAt,            // the system stamp — secondary to ReceivedAt everywhere
    string CreatedByEmail)
{
    public string Reference => $"TEQ-{Number:0000}";

    /// <summary>The date the enquiry is currently working towards: the tender return once
    /// shortlisted, otherwise the PQQ return.</summary>
    public DateTimeOffset? NextDueAt =>
        Status is TenderEnquiryStatus.Shortlisted or TenderEnquiryStatus.TenderSubmitted
            ? TenderDueAt ?? PqqDueAt
            : PqqDueAt ?? TenderDueAt;

    /// <summary>True when a still-open enquiry has sailed past the date it was working towards.</summary>
    public bool IsOverdue =>
        Status.IsOpen()
        && NextDueAt is { } dueAt
        && dueAt < DateTimeOffset.UtcNow
        && !HasMetNextDeadline;

    private bool HasMetNextDeadline => Status switch
    {
        TenderEnquiryStatus.PqqSubmitted => true,
        TenderEnquiryStatus.TenderSubmitted => true,
        _ => false
    };
}

/// <summary>One numbered question on the questionnaire and Jewel's answer to it, in the order the
/// architect asked them. The answers ARE the PQQ response — the official PDF renders these rows.</summary>
public sealed record TenderEnquiryAnswer(
    string TenderEnquiryAnswerId,
    string TenderEnquiryId,
    int Position,
    string Question,
    string Answer);

/// <summary>A rendered PQQ response document, regenerated from the answers on every request —
/// download, email attach — so the file is always the response as it currently stands.</summary>
public sealed record TenderEnquiryDocumentFile(string FileName, string ContentType, byte[] Content);
