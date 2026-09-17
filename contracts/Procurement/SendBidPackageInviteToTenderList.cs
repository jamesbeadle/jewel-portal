using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Emails the tender invite to the package's tender list from the shared mailbox. The mailbox
// itself is the To (subcontractors must not see each other); BCC is every recipient still in the
// running (on the list or Responded — Declined and Won are skipped) with a directory email, or
// exactly the RecipientIds given (2026-09-10, after eleven firms got the invite twice). What
// travels with it — the pricing schedule, the company T&Cs, the package's tender documents and
// its linked drawings — is planned by the assembler, and the draft carries the package's tag
// ("JPMS/BPI-0001") so the sent copy — and replies triaged onto the same tag — group under the
// package. The caller composes Subject/HtmlBody first and this command emails exactly what it is
// given. SaveAsDraftOnly stops after staging, leaving the reviewed draft in the mailbox's Drafts
// folder for Outlook — what this command always did before 2026-09-17, now a choice rather than a
// fate; a failed send degrades to exactly that and says so.
//
// Its sibling SendBidPackageInvite is the tender composer's door: that one takes the To/Cc/Bcc a
// person typed, this one takes the tender list. Both end at the same dispatcher.
public sealed record SendBidPackageInviteToTenderList(
    string BidPackageId,
    string Subject,
    string HtmlBody,
    // The tender-list rows (BidPackageRecipient.RecipientId) to BCC — null/empty means the default
    // set above. An id that does not resolve to a recipient with a directory email is ignored;
    // when none resolve the handler refuses with a readable message.
    IReadOnlyList<string>? RecipientIds = null,
    bool SaveAsDraftOnly = false) : ICommand<BidPackageInviteOutcome>;
