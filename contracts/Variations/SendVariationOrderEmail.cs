using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Variations;

// Emails the variation order's official document from the shared projects mailbox. The recipients
// are the project's client side — its Client and Architect contacts — or one ad-hoc
// RecipientOverride instead; the PDF is rendered fresh from the record, so the attachment is
// byte-for-byte the file the Download button streams.
//
// Only Issued, Awaiting AI and Approved variations may be emailed (VariationOrderStatus.
// IsEmailable): a quoting-stage price has not been put, and a rejected one is terminal.
//
// SaveAsDraftOnly stops after staging, leaving the reviewed draft in the mailbox's Drafts folder
// for Outlook. A failed send degrades to exactly that and says so, so the email is never lost.
public sealed record SendVariationOrderEmail(
    string VariationOrderId,
    string? RecipientOverride = null,
    bool SaveAsDraftOnly = false) : ICommand<VariationOrderEmailOutcome>;

/// <summary>What became of it. Sent=false with no FailureNote is a draft left in the mailbox by
/// choice; Sent=false with one is a send the mailbox refused, and the draft is still there.</summary>
public sealed record VariationOrderEmailOutcome(
    string VariationOrderId,
    string DocumentReference,
    string Subject,
    IReadOnlyList<string> Recipients,
    string? WebLink,
    string? DraftMessageId = null,
    bool Sent = false,
    string? FailureNote = null);
