using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CostCenters.Commands;

public sealed class AddCostCenterHandler : ICommandHandler<AddCostCenter, CostCenter>
{
    private readonly JpmsContext context;

    public AddCostCenterHandler(JpmsContext context) { this.context = context; }

    public async Task<CostCenter> HandleAsync(AddCostCenter command, CancellationToken cancellationToken)
    {
        var code = command.Code.Trim();

        var codeTaken = await context.CostCenters
            .AnyAsync(c => c.Code == code, cancellationToken);
        if (codeTaken) throw new InvalidOperationException($"The code '{code}' is already in use.");

        // SortOrder 0 means "append after the current last code".
        var sortOrder = command.SortOrder;
        if (sortOrder <= 0)
        {
            var maxSortOrder = await context.CostCenters
                .Select(c => (int?)c.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;
            sortOrder = maxSortOrder + 10;
        }

        var entity = new CostCenterEntity
        {
            CostCenterId = CostCenterIdentifierFactory.Next(),
            Code = code,
            Name = command.Name.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };
        context.CostCenters.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
