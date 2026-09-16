using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>Sections 3, 4 and 7 as the register holds them at build time — nothing carried
/// forward, nothing stored on the report.</summary>
internal static class ContractorsReportRegisterReader
{
    private static readonly int[] VariationStatusesStillOpen =
    {
        (int)VariationOrderStatus.Quoting,
        (int)VariationOrderStatus.Issued,
        (int)VariationOrderStatus.AwaitingArchitectInstruction
    };

    /// <summary>Section 3: every RFI on the project not yet closed.</summary>
    public static async Task<IReadOnlyList<ContractorsReportDecision>> DecisionsAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var rows = await context.Requests.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.Kind == (int)RequestType.Rfi && row.Status != (int)RequestStatus.Closed)
            .OrderBy(row => row.Number)
            .ToListAsync(cancellationToken);
        return rows
            .Select(row => new ContractorsReportDecision(
                row.RequestId,
                row.Reference,
                row.Title,
                ((RequestStatus)row.Status).DisplayName(),
                row.ResponseDue is { } due ? DateOnly.FromDateTime(due.Date) : null))
            .ToList();
    }

    /// <summary>Section 4: every variation not yet approved or rejected, at its estimated value
    /// (the agreed value once one is staged).</summary>
    public static async Task<IReadOnlyList<ContractorsReportVariation>> VariationsAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var rows = await context.VariationOrders.AsNoTracking()
            .Where(row => row.ProjectId == projectId && VariationStatusesStillOpen.Contains(row.Status))
            .OrderBy(row => row.Number)
            .ToListAsync(cancellationToken);
        return rows
            .Select(row => new ContractorsReportVariation(
                row.VariationOrderId,
                row.Number > 0 ? $"V{row.Number}" : row.Reference,
                row.Title,
                ((VariationOrderStatus)row.Status).DisplayName(),
                row.EstimatedValue ?? row.Value))
            .ToList();
    }

    /// <summary>Section 7: the standing contact on the project's active Building Control case.</summary>
    public static async Task<ContractorsReportBuildingControl> BuildingControlAsync(
        JpmsContext context, string projectId, string liaison, CancellationToken cancellationToken)
    {
        var activeCase = await context.BuildingControlCases.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.Status != (int)BuildingControlCaseStatus.Lapsed)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return new ContractorsReportBuildingControl(
            Blank(activeCase?.BodyName),
            Blank(activeCase?.ContactName),
            Blank(activeCase?.ContactEmail),
            Blank(activeCase?.ContactPhone),
            liaison);
    }

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
