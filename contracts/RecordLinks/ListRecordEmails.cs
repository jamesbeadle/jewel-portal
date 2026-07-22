using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// Emails currently tagged to ONE record (any linkable type), read live from the mailbox via the
// record-link layer — the per-record generalisation of ListSchedulingEmails. The tag is the only
// association (nothing is stored), so this reflects whatever is tagged right now. Feeds a record
// page's Communications panel (e.g. the VOQ page showing the mail behind the quote and its VO).
public sealed record ListRecordEmails(
    RecordType Type,
    string RecordId) : IQuery<IReadOnlyList<MailboxMessage>>;
