using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Creates the tender-invite email as a DRAFT in the shared mailbox — nothing is sent. The mailbox
// itself is the To (subcontractors must not see each other), every invited recipient with a
// directory email goes in BCC, the package's linked drawings are attached, and the draft carries
// the package's tag ("JPMS/BPI-0001") so the sent copy — and replies triaged onto the same tag —
// group under the package. A person reviews and sends the draft from Outlook; the caller composes
// Subject/HtmlBody in the UI first and this command drafts exactly what it is given.
public sealed record PrepareBidPackageInviteDraft(
    string BidPackageId,
    string Subject,
    string HtmlBody) : ICommand<BidPackageInviteDraft>;
