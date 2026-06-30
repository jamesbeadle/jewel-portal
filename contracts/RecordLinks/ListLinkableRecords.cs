using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// All records of a given type on a project, projected as LinkableRecords for the triage "link to
// existing" panel. Backs the category-first picker: choose a record type + project, get its records.
public sealed record ListLinkableRecords(
    string     ProjectId,
    RecordType Type) : IQuery<IReadOnlyList<LinkableRecord>>;
