namespace Jewel.JPMS.Models;

// What happened, on the record of client-facing interactions. Scope decision 2026-07-22: the audit
// trail records CLIENT-FACING events only — triage decisions on client-pathway threads, client
// records created or linked from email, drafted client correspondence, wall refusals, snapshots.
// Subcontractor/internal event values are reserved (declared, never written yet) so widening the
// scope later is a filter change, not a schema change.
//
// First widening, 2026-07-28: CostCentreRecoded — the finance reconciliation trail. Moving a
// valuation report line between cost centres recodes where money sits in the cost-centre master,
// so each move is recorded (who, when, which line, from → to, the value carried). Not a
// client-facing event: Pathway is "", and the register's client filters simply never match it.
//
// Second widening, 2026-08-04: the link events (EmailTriaged / RecordLinked) are now written for
// EVERY pathway, not just Client — the record activity indicator derives each record's
// recent-communications score from these rows (ListRecordActivity), so links to bid packages,
// work orders and pathway-neutral records must leave a trace too. Pathway carries the thread's
// side ("Subcontractor", "Internal", or "" for neutral links); client-facing views keep filtering
// on it, which is the filter-change-not-schema-change this enum's reserved values planned for.
public enum AuditEventType
{
    EmailTriaged = 0,           // a thread was filed under a pathway via its first link/create
    RecordLinked = 1,           // an email was linked to an existing client record
    RecordCreatedFromEmail = 2, // a client record was created from an email at triage
    TagRemoved = 3,             // a record tag was removed from a client-pathway email
    Discarded = 4,              // a client-pathway thread was discarded
    Restored = 5,               // a discarded thread returned to the queue
    WallRejected = 6,           // an action that would cross the client wall was refused
    DraftCreated = 7,           // the portal drafted client correspondence (request doc / reply)
    SnapshotTaken = 8,          // a valuation report snapshot was frozen (invoice raise)
    BackfillStamped = 9,        // the backfill stamped a pathway onto an existing thread
    // Reserved for the wider scope — declared so persisted ints never shift:
    CrossPathwayOverride = 10,  // a deliberate Subcontractor↔Internal dual filing
    ThreadSwept = 11,           // the queue sweep propagated tags to a late reply
    // Finance reconciliation (written since 2026-07-28):
    CostCentreRecoded = 12,     // a valuation report line moved to a different cost centre
    // Outbound mail (written since the triage compose work, 2026-08-04). EmailSent is always
    // material whatever the pathway — the portal put words in front of a correspondent — so it is
    // written for every send, with Pathway carrying the thread's side ("" when none was chosen).
    EmailSent = 13,             // the portal sent an email from the projects mailbox
    EmailSendFailed = 14,       // a send attempt failed after the draft was staged (draft kept)
    // Project lifecycle (written since the settings danger-zone work, 2026-08-05). The project's
    // own rows are gone by the time this is written, so RecordReference carries the deleted
    // project's reference and the detail its name — the event is the surviving record.
    ProjectDeleted = 15         // a project and its records were permanently deleted from settings
}

// One append-only audit event. WebLink (when present) opens the email or draft in Outlook on the
// web — the audit register doubles as the index for finding portal-drafted mail in Outlook.
public sealed record AuditEvent(
    string AuditEventId,
    DateTimeOffset OccurredAt,
    string ActorEmail,
    AuditEventType EventType,
    // Short pathway label ("Client", "Subcontractor", "Internal") or "" when not pathway-specific.
    string Pathway,
    string? ProjectId,
    RecordType? RecordType,
    string? RecordId,
    // Denormalised display reference (e.g. "RFI-012", "REQ-0113", "VI-0004") so rows render without joins.
    string RecordReference,
    string? ConversationId,
    string? EmailMessageId,
    string? InternetMessageId,
    string? WebLink,
    // One human sentence: "Linked to RFI-012", "RFI-012 document drafted to …", etc.
    string Detail);

// One page of the audit register, newest first. Cursor is a plain offset.
public sealed record AuditEventsPage(
    IReadOnlyList<AuditEvent> Items,
    string? NextCursor,
    int Total);
