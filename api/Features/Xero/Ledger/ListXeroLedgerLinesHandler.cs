using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed class ListXeroLedgerLinesHandler : IQueryHandler<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>>
{
    private readonly JpmsContext context;

    public ListXeroLedgerLinesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<XeroLedgerLine>> HandleAsync(ListXeroLedgerLines query, CancellationToken cancellationToken)
    {
        var entities = await context.XeroLedgerLines.AsNoTracking()
            .OrderByDescending(line => line.Date)
            .ToListAsync(cancellationToken);

        var projects = await context.Projects.AsNoTracking().ToListAsync(cancellationToken);
        var costCenters = await context.CostCenters.AsNoTracking()
            .Where(centre => centre.IsActive)
            .ToListAsync(cancellationToken);
        var suggester = new XeroAllocationSuggester(projects, costCenters);

        return entities.Select(entity => ToModel(entity, suggester)).ToList();
    }

    private static XeroLedgerLine ToModel(XeroLedgerLineEntity entity, XeroAllocationSuggester suggester)
    {
        // Suggestions only matter while a line still needs a decision.
        var unallocated = entity.AllocationStatus == (int)XeroAllocationStatus.Unallocated;
        return new XeroLedgerLine(
            entity.XeroLedgerLineId,
            entity.XeroInvoiceId,
            entity.Type,
            entity.InvoiceNumber,
            entity.Reference,
            entity.ContactName,
            entity.Date,
            entity.InvoiceStatus,
            entity.Description,
            entity.Net,
            entity.Tax,
            entity.AccountCode,
            entity.AccountName,
            entity.XeroSite,
            entity.XeroCostCode,
            (XeroAllocationStatus)entity.AllocationStatus,
            entity.ProjectId,
            entity.CostCenterCode,
            entity.Bucket,
            entity.AllocatedBy,
            entity.AllocatedAtUtc,
            entity.Note,
            unallocated ? suggester.SuggestProject(entity.XeroSite) : null,
            unallocated ? suggester.SuggestCostCenter(entity.XeroCostCode) : null,
            unallocated ? suggester.SuggestBucket(entity.ContactName, entity.Description) : null,
            entity.FirstSeenAtUtc,
            entity.LastSyncedAtUtc);
    }
}
