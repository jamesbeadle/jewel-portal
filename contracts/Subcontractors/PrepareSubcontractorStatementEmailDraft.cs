using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

// Drafts the statement-of-account email to the subcontractor in the shared mailbox — nothing is
// sent; a human reviews and sends it from Outlook, matching the work-order and tender-invite
// convention. The recipient is the subcontractor's directory email. The statement itself is
// rendered server-side from the live register and attached as a PDF, so the figures in the
// attachment always match the register at the moment of drafting; the subject and HTML cover
// note are the caller's to edit before drafting.
public sealed record PrepareSubcontractorStatementEmailDraft(
    string SubcontractorId,
    string Subject,
    string HtmlBody) : ICommand<SubcontractorStatementEmailDraft>;
