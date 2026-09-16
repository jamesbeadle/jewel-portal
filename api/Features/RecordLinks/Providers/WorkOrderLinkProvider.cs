using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Procurement;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for work orders (the purchase order Jewel places with a subcontractor
// or, since 2026-09-15, a materials/goods supplier). Wraps the WorkOrders table so a triage email
// can be linked to an order and the order can read its mail back live by tag (RecordEmailReader) —
// the same mechanism the Bid Package family uses, with no changes to the link/read layer or triage UI.
//
// The pathway FOLLOWS THE COMPANY the order is placed with (CompanyPathways): the same record
// is offered on the Subcontractor pane and the Supplier pane, so the type alone cannot say which
// side a thread belongs to. Every record this provider hands out therefore carries
// LinkableRecord.Pathway — "Supplier" for a Supplier-category company, "Subcontractor" for any
// other — and the link layer reads it through TriageCategories.BucketFor(LinkableRecord). (The
// defect joined this road on 2026-09-16 — DefectLinkProvider — with one difference: its type is
// pathway-neutral, so the pane that stages the tag can override the company.)
//
// The tag stem is PROJECT-QUALIFIED ("JBB-2026-001-WO-0045", WorkOrderTags) since 2026-09-14: order
// numbers are per project, so the flat "WO-0045" named By France's Farrant order AND Coombe Lane's
// migrated Hamilton Glass order, and whichever row the database returned first won the resolve.
public sealed class WorkOrderLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
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
        var projectRef = await WorkOrderTags.ProjectRefAsync(context, projectId, ct);
        var rows = await WithSupplierAsync(
            context.WorkOrders.AsNoTracking()
                .Where(o => o.ProjectId == projectId
                            && o.Status != (int)WorkOrderStatus.Draft)
                .OrderByDescending(o => o.Number),
            ct);
        return rows.Select(row => ToLinkable(row.Order, row.Company, projectRef)).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var rows = await WithSupplierAsync(
            context.WorkOrders.AsNoTracking().Where(o => o.WorkOrderId == recordId),
            ct);
        return rows.Count == 0 ? null : await ToLinkableAsync(rows[0].Order, rows[0].Company, ct);
    }

    // Reverse lookup for the tag chips and the Control Centre's "use the thread's existing tags":
    // "JBB-2026-001-WO-0045" names the order numbered 45 on JBB-2026-001 — the candidates carrying
    // that number are verified against their own full qualified stem, which is what tells two
    // projects' 0045 apart. The legacy flat stem ("WO-0045", mail tagged before 2026-09-14) is
    // accepted only when exactly ONE order carries the number; two or more is an answer nobody
    // should guess, so it resolves to nothing and the triager picks by hand (the retag sweep,
    // RetagWorkOrderWorkflowTags, moves such mail onto qualified stems). Drafts have no number so
    // their (unstable) id-derived stems never parse here — the same reason ForProjectAsync
    // excludes them.
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (WorkOrderTags.NumberOf(tagReference) is not { } number) return null;
        var candidates = await WithSupplierAsync(
            context.WorkOrders.AsNoTracking().Where(o => o.Number == number).Take(10),
            ct);

        if (WorkOrderTags.IsLegacyStem(tagReference))
            return candidates.Count == 1 ? await ToLinkableAsync(candidates[0].Order, candidates[0].Company, ct) : null;

        foreach (var candidate in candidates)
        {
            var record = await ToLinkableAsync(candidate.Order, candidate.Company, ct);
            if (record.TagReference.Equals(tagReference, StringComparison.OrdinalIgnoreCase))
                return record;
        }
        return null;
    }

    // The company alongside each order: its name (the discriminator a triager reads first) and
    // its directory category (which side the order's mail files under). One projection, no
    // per-row queries; both null when the order points at no directory record.
    private async Task<List<(WorkOrderEntity Order, WorkOrderCompany? Company)>> WithSupplierAsync(
        IQueryable<WorkOrderEntity> orders, CancellationToken ct)
    {
        var rows = await orders
            .Select(order => new
            {
                Order = order,
                Company = context.Subcontractors.AsNoTracking()
                    .Where(s => s.SubcontractorId == order.SubcontractorId)
                    .Select(s => new { s.CompanyName, s.Category })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);
        return rows
            .Select(row => (
                Order: row.Order,
                Company: row.Company is null ? (WorkOrderCompany?)null : new WorkOrderCompany(row.Company.CompanyName, row.Company.Category)))
            .ToList();
    }

    private sealed record WorkOrderCompany(string CompanyName, int Category);

    private async Task<LinkableRecord> ToLinkableAsync(WorkOrderEntity entity, WorkOrderCompany? company, CancellationToken ct) =>
        ToLinkable(entity, company, await WorkOrderTags.ProjectRefAsync(context, entity.ProjectId, ct));

    private static LinkableRecord ToLinkable(WorkOrderEntity entity, WorkOrderCompany? company, string? projectRef)
    {
        // The order's sequential WO-0001 reference is what people say; the tag stem is that
        // reference qualified by the project (WorkOrderTags), so an email tagged
        // "JPMS/JBB-2026-001-WO-0001" surfaces under this project's order and no other's. Seeded
        // Buildertrend orders keep their PO number in the project's sequence, and legacy rows with
        // no Number fall back to the id-derived stem (WorkOrderEntity.Reference handles both).
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
            TagReference: WorkOrderTags.Stem(projectRef, entity.ProjectId, reference),
            Title:        title,
            StatusLabel:  ((WorkOrderStatus)entity.Status).ToString(),
            Summary:      RecordSummaries.Clip(company?.CompanyName),
            // Released is the one live state; Complete, Cancelled and Rejected are finished business.
            IsActive:     entity.Status == (int)WorkOrderStatus.Released,
            // The side this order's mail files under follows its company (see the class note).
            Pathway:      CompanyPathways.LabelFor(company is null ? (DirectoryCategory?)null : (DirectoryCategory)company.Category));
    }
}
