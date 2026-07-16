using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// Emails currently tagged to a project's scheduling bucket ("JPMS/SCH-<projectRef>"), read live by
// tag via the record-link layer. The tag is the only association — nothing is stored — so this
// reflects whatever is tagged to scheduling now. Feeds the Programme tab's Communications view.
public sealed record ListSchedulingEmails(string ProjectId) : IQuery<IReadOnlyList<MailboxMessage>>;
