using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// SENDS the purchase-order email to the supplier from the shared projects mailbox — the automatic
// counterpart of PrepareWorkOrderEmailDraft (which stages a reviewed draft for Outlook and remains
// for re-sends and edits). Fired by the UI straight after a work order is released — created
// without "save as draft" (Work Orders tab or Control Centre), or a draft approved from the Work
// Orders tab — with the user warned beforehand that the email will go out.
//
// The subject/body are composed client-side (WorkOrderPoEmail), same as the Prepare flow, because
// the portal-acceptance link needs the app's own base URI. Failure ordering mirrors triage compose:
// the draft is staged with its record tags first and the SEND is the last step, so a failed send
// leaves the reviewed draft in the mailbox's Drafts folder (outcome Sent=false + WebLink) and the
// order itself is never affected — the email can be finished from Outlook or re-sent from the PO
// page. A draft or rejected order is refused outright, same promise as the Prepare handler.
public sealed record SendWorkOrderPoEmail(
    string WorkOrderId,
    string Subject,
    string HtmlBody) : ICommand<WorkOrderPoEmailOutcome>;
