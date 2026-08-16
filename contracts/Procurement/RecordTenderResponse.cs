using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// The Control Centre's "File Bid Package Tender" action: an email carrying a subcontractor's tender
// has been filed to the package (the tag is the filing — this command never links mail), so mark
// the matching recipient Responded — their tender IS their response, whether or not its prices have
// been extracted into a quote yet. The sender is matched to the tender list by exact directory
// email, else by a unique company domain (freemail domains never match); no match is a quiet no-op,
// not a failure — the email is still filed and the submission can be recorded by hand. Returns the
// package's full recipient list. SaveExtractedQuote later re-affirms Responded idempotently.
public sealed record RecordTenderResponse(
    string BidPackageId,
    string SenderEmail) : ICommand<IReadOnlyList<BidPackageRecipient>>;
