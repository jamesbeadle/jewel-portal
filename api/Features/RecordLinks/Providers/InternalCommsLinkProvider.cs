using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Provider for the "internal communication" records — staff-to-staff correspondence tied to no
// to-do, bid package or work order: the general tag plus its categories (Site instruction,
// Build-up, Spec note — Nigel, 2026-08-22). The same table-less shape as the Subcontractor family:
// the records are the constants in contracts (InternalComms.All), the tag on the email IS the
// association, and Internal → Communications reads the mail back live by those tags. Not
// project-scoped; TriageCategories.BucketFor files the thread under JPMS/Internal.
public sealed class InternalCommsLinkProvider : ILinkableRecordProvider
{
    public RecordType Type => RecordType.InternalComms;

    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { InternalComms.Reference };

    public Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct) =>
        Task.FromResult(InternalComms.All);

    public Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct) =>
        Task.FromResult(InternalComms.Find(recordId));
}
