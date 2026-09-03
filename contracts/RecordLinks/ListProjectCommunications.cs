using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// Every email tagged to one of a project's records, read live from the mailbox via the record-link
// layer — the project-wide generalisation of ListSchedulingEmails. The tag is the only association
// (nothing is stored), so this reflects whatever is tagged right now. Type narrows the read to one
// record type's tags (e.g. CostCentre → only cost-centre-tagged mail); null reads every linkable
// type. Cursor-paged like the triage views. Feeds the project's Communications tab.
public sealed record ListProjectCommunications(
    string ProjectId,
    RecordType? Type = null,
    string? Cursor = null,
    int Take = 25,
    // Narrow to one communication pathway: "Client", "Subcontractor" or "Internal" (short labels).
    // Applied server-side against each message's bucket category; null reads every pathway. When
    // set, Total is 0 ("count unknown") — the filter is applied per page, so the full count isn't
    // known without walking the stream.
    string? Bucket = null,
    // Free-text search WITHIN the project's tagged mail (2026-09-03): a Graph $search over the
    // mailbox (subjects, bodies, senders, attachment names — never categories) whose hits are
    // then kept only when they carry one of the project's tags, so a search can never surface
    // another project's email. Relevance-ordered, one page (Graph's $search cannot page or
    // combine with a category filter): NextCursor is null and Total is the hit count. Type and
    // Bucket still narrow the hits. Null = the paged tag read.
    string? Search = null) : IQuery<ProjectCommunicationsPage>;
