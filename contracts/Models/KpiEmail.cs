namespace Jewel.JPMS.Models;

// A person KPIs can be filed under (2026-09-03): anyone at Jewel — a portal user (linked by their
// sign-in email, name snapshotted) or someone added by name alone (James asked the same day:
// "employees that don't have a user, add someone manually like james clark"). One row per
// person; the register groups and filters by it. Administrators only, like everything KPI.
public sealed record KpiPerson(
    string KpiPersonId,
    string Name,
    // The portal sign-in email when the person is (or was) a portal user; null for someone
    // added by name.
    string? Email,
    int KpiCount = 0);

// An email marked as a KPI against one person (2026-09-03) — evidence of how someone at Jewel is
// performing, filed under that person and readable by ADMINISTRATORS ONLY. Marked from the
// Control Centre's Internal pane (or over the connector). Deliberately NOT a record-link: the
// shared projects@ mailbox never carries a KPI tag, so nobody triaging the queue can see that an
// email was marked, let alone whose it is — the mark lives in the database alone, with a snapshot
// of the email's envelope so the register reads without touching the mailbox. The message ids are
// kept so the email itself can still be opened live when an administrator asks.
public sealed record KpiEmail(
    string KpiEmailId,
    // The person the KPI is filed under (KpiPerson), with their name and portal email as read
    // at list time.
    string PersonId,
    string PersonName,
    string? PersonEmail,
    // The mailbox message: live Graph id (may change if the email moves folders), the stable
    // internet message id for re-finding it, and its conversation id.
    string MessageId,
    string? InternetMessageId,
    string? ConversationId,
    // Envelope snapshot taken at mark time.
    string Subject,
    string FromEmail,
    string FromName,
    DateTimeOffset ReceivedAt,
    // Why it's a KPI — optional, free text.
    string Note,
    string MarkedByEmail,
    DateTimeOffset MarkedAt,
    // Sequential human reference ("KPI-0001"). Not a mailbox tag stem — nothing is tagged.
    // Defaulted last; the server always mints it.
    string Reference = "");
