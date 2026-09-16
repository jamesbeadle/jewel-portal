using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Contracts.Closeout;

/// <summary>
/// Sends a defect to its supplier from the projects mailbox — the defect page's "Send to
/// supplier" / "Chase supplier", performed server-side for the connector (2026-09-16). The
/// email goes to the defect's supplier address, filed under the defect (its JPMS/DEF-#### tag,
/// on the supplier's side of the pathways), so the sent copy and the supplier's reply both
/// land on the defect, and the send itself stamps SentToSupplierAt and moves Open → In progress
/// (DefectSupplierSendRecorder). Subject and Body null take the portal's own wording
/// (DefectSupplierEmails: the first send, or the chase once it has been sent); either given
/// replaces it, as the page's composer lets the sender edit before sending.
/// </summary>
public sealed record SendDefectToSupplier(
    string DefectId,
    string? Subject = null,
    string? Body = null,
    // Stage the email in the mailbox's Drafts folder for a person to send from Outlook, instead of sending.
    bool SaveAsDraftOnly = false,
    // Stamped server-side from the signed-in user; the client cannot spoof it.
    string SentByEmail = "") : ICommand<ComposeOutcome>;
