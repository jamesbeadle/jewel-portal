using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Inventory.Queries;

public sealed class ListInventoryForProjectHandler : IQueryHandler<ListInventoryForProject, IReadOnlyList<InventoryItem>>
{
    private readonly JpmsContext context;
    public ListInventoryForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<InventoryItem>> HandleAsync(ListInventoryForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.InventoryItems.AsNoTracking().Where(item => item.ProjectId == query.ProjectId).OrderByDescending(item => item.Number).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
