using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for work orders (the purchase order Jewel places with a subcontractor).
// Wraps the WorkOrders table so a triage email can be linked to an order and the order can read its
// mail back live by tag (RecordEmailReader) — the same mechanism the Bid Package family uses, with no
// changes to the link/read layer or triage UI.
//
// Subcontract-side by construction: docs/Pathway-Split-Platform-Flow-Plan.md §2.2 lists "link work
// order" in the Subcontractor pathway's action set, and TriageCategories.BucketFor maps the type to
// JPMS/Subcontractor — so an order can never be reached from a Client thread (the wall rejects it).
public sealed class WorkOrderLinkProvider : ILinkableRecordProvider
{
    private readonly JpmsContext context;

    public WorkOrderLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.WorkOrder;

    // Work orders own the "WO" reference namespace.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "WO" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        // The supplier's company name is the discriminator a triager reads first ("which of the four
        // WOs on this project is the flooring one?"), so it is resolved alongside the order rather
        // than left to the title. One extra projection, no per-row queries.
        // Drafts stay excluded until approved: a draft has no number, so its tag stem would be
        // the id fallback ("WO-A1B2C3D4") — and the moment approval mints the real number the
        // stem would change, silently detaching any mail already tagged against it. A REJECTED
        // draft's stem can never change (no approval is coming), so rejected orders ARE listed —
        // flagged inactive, behind the pickers' "include closed / inactive" checkbox.
        var rows = await context.WorkOrders.AsNoTracking()
            .Where(o => o.ProjectId == projectId
                        && o.Status != (int)WorkOrderStatus.Draft)
            .OrderByDescending(o => o.Number)
            .Select(o => new
            {
                Order = o,
                CompanyName = context.Subcontractors.AsNoTracking()
                    .Where(s => s.SubcontractorId == o.SubcontractorId)
                    .Select(s => s.CompanyName)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);
        return rows.Select(row => ToLinkable(row.Order, row.CompanyName)).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var row = await context.WorkOrders.AsNoTracking()
            .Where(o => o.WorkOrderId == recordId)
            .Select(o => new
            {
                Order = o,
                CompanyName = context.Subcontractors.AsNoTracking()
                    .Where(s => s.SubcontractorId == o.SubcontractorId)
                    .Select(s => s.CompanyName)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);
        return row is null ? null : ToLinkable(row.Order, row.CompanyName);
    }

    private static LinkableRecord ToLinkable(WorkOrderEntity entity, string? companyName)
    {
        // The order's sequential WO-0001 reference is the tag stem, so an email tagged to it
        // ("JPMS/WO-0001") surfaces under the order. Seeded Buildertrend orders keep their PO number
        // in that same sequence, and legacy rows with no Number fall back to the id-derived stem
        // (WorkOrderEntity.Reference handles both).
        var reference = entity.Reference;

        // Orders raised straight from an award can carry an empty Title; the scope is the next-best
        // thing to show in the picker so a row is never blank.
        var title = string.IsNullOrWhiteSpace(entity.Title)
            ? RecordSummaries.Clip(entity.Scope) ?? reference
            : entity.Title;

        return new LinkableRecord(
            Type:         RecordType.WorkOrder,
            RecordId:     entity.WorkOrderId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        title,
            StatusLabel:  ((WorkOrderStatus)entity.Status).ToString(),
            Summary:      RecordSummaries.Clip(companyName),
            // Released is the one live state; Complete, Cancelled and Rejected are finished business.
            IsActive:     entity.Status == (int)WorkOrderStatus.Released);
    }
}
