using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Provider for the "supplier communication" records — correspondence with a materials/goods
// supplier, as distinct from a subcontractor (the Control Centre's pathway restructure,
// 2026-08-27): the general tag plus its categories (Materials, re-homed from the subcontractor
// family with its SubComms-Mats stem intact). The same table-less shape as the other comm
// families: the records are the constants in contracts (SupplierComms.All), the tag on the email
// IS the association, and Supplier → Communications reads the mail back live by those tags. Not
// project-scoped; TriageCategories.BucketFor files the thread under JPMS/Supplier.
public sealed class SupplierCommsLinkProvider : ILinkableRecordProvider
{
    public RecordType Type => RecordType.SupplierComms;

    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { SupplierComms.Reference };

    public Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct) =>
        Task.FromResult(SupplierComms.All);

    public Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct) =>
        Task.FromResult(SupplierComms.Find(recordId));
}
