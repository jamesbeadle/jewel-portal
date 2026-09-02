using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private static AiTool GetPackageReconciliation()
    {
        return new(
            "get_package_reconciliation",
            "A project's package reconciliation in one answer: the saved packages (each a named "
            + "group of work orders and sales slices, with its lock state — locked packages "
            + "freeze their figures at lock) and the per-package report rows: sales value, "
            + "claimed to date, target cost, WO committed, invoiced to date, drawdown (budget "
            + "left to commit), margin (live forecast buying gain) and the profit/loss realised "
            + "on lock. The save_reconciliation_package and set_reconciliation_package_lock "
            + "actions act on exactly this — read it first.",
            AiToolSchema.Object(
                ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
            AiToolKind.Read,
            ReconciliationReaders,
            GetPackageReconciliationAsync);
    }

    private static async Task<string> GetPackageReconciliationAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId)) return Fail(NoProject);

        var packages = await Query<ListReconciliationPackagesForProject, IReadOnlyList<ReconciliationPackage>>(
            context, new ListReconciliationPackagesForProject(projectId), ct);
        var rows = await Query<ListPackageReconciliation, IReadOnlyList<PackageReconciliationRow>>(
            context, new ListPackageReconciliation(projectId), ct);
        return Serialise(new
        {
            ok = true,
            projectId,
            packages = packages.Select(PackageRow),
            reconciliation = rows
        });
    }

    private static object PackageRow(ReconciliationPackage package) => new
    {
        package.ReconciliationPackageId,
        package.Name,
        package.WorkOrderIds,
        salesLines = package.SalesLines,
        costLines = package.DirectCosts,
        package.IsLocked,
        package.LockedAt
    };
}
