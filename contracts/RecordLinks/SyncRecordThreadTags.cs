using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// Catch-up: re-scan the threads a record is already linked to and tag any Inbox replies that arrived
// after the original link, so the record's tag always spans the whole, current conversation. Safe to
// call whenever a record is opened / its mail is read — it only tags genuinely-new messages.
public sealed record SyncRecordThreadTags(
    RecordType Type,
    string     RecordId) : ICommand<Acknowledgement>;
