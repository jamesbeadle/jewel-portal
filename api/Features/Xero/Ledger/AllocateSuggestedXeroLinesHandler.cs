using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// One-shot "allocate everything that matched": recomputes suggestions for all
/// unallocated lines with the same suggester the queue uses, and allocates the
/// lines where both the project and the cost centre resolved. Lines with a
/// partial or missing match are left in the queue for a human. The note marks
/// them auto-matched so they remain identifiable (and bulk-reversible) later.
/// </summary>
public sealed class AllocateSuggestedXeroLinesHandler : ICommandHandler<AllocateSuggestedXeroLines, int>
{
    public const string AutoMatchedNote = "Auto-matched from Xero tracking";

    private readonly JpmsContext context;
    private readonly IXeroWriteBackService writeBack;

    public AllocateSuggestedXeroLinesHandler(JpmsContext context, IXeroWriteBackService writeBack)
    {
        this.context = context;
        this.writeBack = writeBack;
    }

    public async Task<int> HandleAsync(AllocateSuggestedXeroLines command, CancellationToken cancellationToken)
    {
        var projects = await context.Projects.AsNoTracking().ToListAsync(cancellationToken);
        var costCenters = await context.CostCenters.AsNoTracking()
            .Where(centre => centre.IsActive)
            .ToListAsync(cancellationToken);
        var suggester = new XeroAllocationSuggester(projects, costCenters);

        var unallocated = await context.XeroLedgerLines
            .Where(line => line.AllocationStatus == (int)XeroAllocationStatus.Unallocated)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var allocated = 0;
        var touchedInvoiceIds = new HashSet<string>();
        foreach (var line in unallocated)
        {
            var projectId = suggester.SuggestProject(line.XeroSite);
            var costCenterCode = suggester.SuggestCostCenter(line.XeroCostCode);
            if (projectId is null || costCenterCode is null) continue;

            line.AllocationStatus = (int)XeroAllocationStatus.Allocated;
            line.ProjectId = projectId;
            line.CostCenterCode = costCenterCode;
            line.AllocatedBy = command.AllocatedBy;
            line.AllocatedAtUtc = now;
            line.Note = AutoMatchedNote;
            allocated++;
            touchedInvoiceIds.Add(line.XeroInvoiceId);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Confirm-and-approve any draft invoice these allocations completed (best-effort;
        // outcomes are stamped onto the lines and visible on the allocation page).
        if (touchedInvoiceIds.Count > 0)
            await writeBack.TryWriteBackAsync(touchedInvoiceIds, cancellationToken);

        return allocated;
    }
}
