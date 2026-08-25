using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.TenderEnquiries;

/// <summary>
/// The editable face of a tender enquiry — everything a person types about the invitation, as
/// one value so logging and editing carry the same fields and the same validation. Dates are
/// dates: the times of day on a return deadline are the architect's business, not ours.
/// </summary>
public sealed record TenderEnquiryDetails(
    string Title,
    string ArchitectPracticeName,
    string ArchitectContactName,
    string ArchitectContactEmail,
    string ScopeSummary,
    string ContractForm,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? PqqDueAt,
    DateTimeOffset? TenderDueAt);

/// <summary>
/// The Lead-stage project shell an enquiry creates when it is the first Jewel has heard of the
/// job. Reference is minted server-side from the organisation and year; the architect becomes
/// the project's correspondent party (found by practice name, or created).
/// </summary>
public sealed record TenderEnquiryProjectDraft(
    string Name,
    string ClientName,
    Organisation Organisation,
    string AddressLine,
    string Town,
    string Postcode);

/// <summary>One question/answer pair as typed in the PQQ editor; positions are re-minted 1..n
/// on save from the order the rows arrive in.</summary>
public sealed record TenderEnquiryAnswerDraft(string Question, string Answer);
