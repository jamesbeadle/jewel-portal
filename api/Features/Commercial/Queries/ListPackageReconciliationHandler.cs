using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListPackageReconciliationHandler
    : IQueryHandler<ListPackageReconciliation, IReadOnlyList<PackageReconciliationRow>>
{
    private readonly JpmsContext context;

    public ListPackageReconciliationHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PackageReconciliationRow>> HandleAsync(
        ListPackageReconciliation query, CancellationToken cancellationToken) =>
        await PackageReconciliationCalculator.ComputeAsync(context, query.ProjectId, cancellationToken);
}
