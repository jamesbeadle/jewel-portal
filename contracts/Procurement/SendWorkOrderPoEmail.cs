using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Emails the purchase order to the supplier from the shared projects mailbox — the ONE command for
// every route that emails a work order: the PO page's "Email to supplier…", the automatic send
// fired when an order is released (created without "save as draft" on the Work Orders tab or the
// Control Centre, or a draft approved), and the tender award's email to the winner. The separate
// PrepareWorkOrderEmailDraft command was folded in here on 2026-09-17: one email had become two,
// and only one of them sent, attached the PDF or left an audit row.
//
// SaveAsDraftOnly stops after staging, leaving the reviewed draft in the mailbox's Drafts folder
// for Outlook — the award door's old behaviour, kept as an explicit choice rather than a fate.
//
// The subject/body are composed client-side (WorkOrderPoEmail) so every door pre-fills the same
// words. Failure ordering is the dispatcher's: the draft is staged with its record tags first and
// the SEND is the last step, so a failed send leaves the reviewed draft in Drafts (outcome
// Sent=false + WebLink + FailureNote) and the order itself is never affected. A draft or rejected
// order is refused outright.
public sealed record SendWorkOrderPoEmail(
    string WorkOrderId,
    string Subject,
    string HtmlBody,
    bool SaveAsDraftOnly = false) : ICommand<WorkOrderPoEmailOutcome>;
