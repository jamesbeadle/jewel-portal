using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Provider for the single "subcontractor communication" record — general subcontract-side
// correspondence tied to no bid package, work order or defect. There is no table behind it: the
// record is the constant defined in contracts (SubcontractorComms), the tag on the email IS the
// whole association, and the Subcontractor → Communications page reads the mail back live by that
// tag. Not project-scoped — the same record is offered whatever project the triager picked, and
// linking works with no project at all. TriageCategories.BucketFor maps the type to
// JPMS/Subcontractor, so tagging a communication files the thread subcontract-side like every
// other record on that pathway (client wall and lanes included).
public sealed class SubcontractorCommsLinkProvider : ILinkableRecordProvider
{
    public RecordType Type => RecordType.SubcontractorComms;

    // The tag stem is the whole reference ("JPMS/SubComms") — this provider owns it outright.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { SubcontractorComms.Reference };

    public Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<LinkableRecord>>(new[] { SubcontractorComms.Record });

    public Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct) =>
        Task.FromResult<LinkableRecord?>(
            recordId == SubcontractorComms.RecordId ? SubcontractorComms.Record : null);
}
