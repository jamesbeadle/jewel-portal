using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Provider for the "subcontractor communication" records — general subcontract-side correspondence
// tied to no bid package, work order or defect: the general tag plus its categories (Chaser, Info
// request, Materials, H&S — decision 2026-08-17). There is no table behind any of them: the records
// are the constants defined in contracts (SubcontractorComms.All), the tag on the email IS the whole
// association, and the Subcontractor → Communications page reads the mail back live by those tags.
// Not project-scoped — the same records are offered whatever project the triager picked, and linking
// works with no project at all. TriageCategories.BucketFor maps the type to JPMS/Subcontractor, so
// tagging a communication files the thread subcontract-side like every other record on that pathway
// (client wall and lanes included).
public sealed class SubcontractorCommsLinkProvider : ILinkableRecordProvider
{
    public RecordType Type => RecordType.SubcontractorComms;

    // Every stem in the family starts "SubComms" (the categories extend it with "-Chase" etc.), so
    // the one prefix owns the whole namespace outright.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { SubcontractorComms.Reference };

    public Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct) =>
        Task.FromResult(SubcontractorComms.All);

    public Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct) =>
        Task.FromResult(SubcontractorComms.Find(recordId));
}
