using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CostCenters.Commands;

public sealed class ReviseCostCenterHandler : ICommandHandler<ReviseCostCenter, CostCenter>
{
    private readonly JpmsContext context;

    public ReviseCostCenterHandler(JpmsContext context) { this.context = context; }

    public async Task<CostCenter> HandleAsync(ReviseCostCenter command, CancellationToken cancellationToken)
    {
        var entity = await context.CostCenters
            .SingleOrDefaultAsync(c => c.CostCenterId == command.CostCenterId, cancellationToken);
        if (entity is null) throw new KeyNotFoundException($"Cost centre '{command.CostCenterId}' was not found.");

        var code = command.Code.Trim();
        var codeTaken = await context.CostCenters
            .AnyAsync(c => c.Code == code && c.CostCenterId != command.CostCenterId, cancellationToken);
        if (codeTaken) throw new InvalidOperationException($"The code '{code}' is already in use.");

        entity.Code = code;
        entity.Name = command.Name.Trim();
        entity.SortOrder = command.SortOrder;
        entity.IsActive = command.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
