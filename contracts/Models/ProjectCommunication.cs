namespace Jewel.JPMS.Models;

// One email on a project's Communications tab: the mailbox message plus the project records it is
// tagged to, resolved server-side so the tab can label each row ("Cost centre 0001 — Contract
// Works", "RFI-012 — Steel connection detail") without a per-row lookup. An email can carry several
// workflow tags at once — the same message can feed a request AND a cost centre — so Links is a list.
public sealed record ProjectCommunication(
    MailboxMessage Message,
    IReadOnlyList<ProjectCommunicationLink> Links);

// A single resolved tag on a communication: which record type it points at, the record's human
// reference and title for display, and the full mailbox tag itself (e.g. "JPMS/CC-JBB-2026-001-0001")
// so the raw tag can be shown or matched client-side.
public sealed record ProjectCommunicationLink(
    RecordType Type,
    string Reference,
    string Title,
    string Tag);

// One page of a project's tagged communications. Mirrors MailboxPage: Graph pages with an opaque
// cursor (NextCursor null on the last page) and Total is the count of all messages matching the
// filter, not just this page.
public sealed record ProjectCommunicationsPage(
    IReadOnlyList<ProjectCommunication> Items,
    string? NextCursor,
    int Total);
