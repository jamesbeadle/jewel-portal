using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

/// <summary>Soft, stamped, idempotent: archiving an archived item keeps the FIRST stamp — the
/// history says when it actually left the grid.</summary>
public sealed class ArchiveWeeklyCashflowItemHandler : ICommandHandler<ArchiveWeeklyCashflowItem, WeeklyCashflowItem>
{
    private readonly JpmsContext context;

    public ArchiveWeeklyCashflowItemHandler(JpmsContext context) { this.context = context; }

    public async Task<WeeklyCashflowItem> HandleAsync(ArchiveWeeklyCashflowItem command, CancellationToken cancellationToken)
    {
        var entity = await context.WeeklyCashflowItems
            .FirstOrDefaultAsync(item => item.WeeklyCashflowItemId == command.WeeklyCashflowItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Weekly cashflow item '{command.WeeklyCashflowItemId}' not found.");

        if (entity.ArchivedAt is null)
        {
            entity.ArchivedAt = DateTimeOffset.UtcNow;
            entity.ArchivedByEmail = command.ArchivedByEmail;
            await context.SaveChangesAsync(cancellationToken);
        }
        return entity.ToModel();
    }
}
