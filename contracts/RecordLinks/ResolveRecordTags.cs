using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// Resolves mailbox tag stems (the text after "JPMS/" in an email's categories — "TODO-0011",
// "BPI-0004", "JBB-2026-001-RFI-012") back to the records they name, so a UI holding only an
// email's tags can offer "open the record" without knowing any type's reference grammar. Feeds the
// tagged-email search on the to-do surfaces: search the mailbox, read each hit's tags, resolve
// them here, render each resolved tag as a link to the record's own page. Stems that resolve to
// nothing (a deleted record, a workflow tag like "Discarded", an unknown family) are simply absent
// from the answer — an unresolved chip renders as plain text, never an error. Triage-gated, like
// the mailbox search it rides on.
public sealed record ResolveRecordTags(
    IReadOnlyList<string> Tags) : IQuery<IReadOnlyList<LinkableRecord>>;
