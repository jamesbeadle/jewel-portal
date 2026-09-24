using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>Sections 3, 4 and 7 as the register holds them at build time — nothing carried
/// forward, nothing stored on the report.</summary>
internal static class ContractorsReportRegisterReader
{
    /// <summary>Section 3: every RFI on the project put to the client and not yet closed — an RFI at
    /// Needs action is still being checked in-house and is not printed.</summary>
    public static async Task<IReadOnlyList<ContractorsReportDecision>> DecisionsAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var rows = await context.Requests.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.Kind == (int)RequestType.Rfi
                && row.Status != (int)RequestStatus.Closed && row.Status != (int)RequestStatus.NeedsAction)
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

    /// <summary>Section 4: every variation Issued and unanswered, with the date it was issued —
    /// not Quoting (the client has not seen it) and not Awaiting AI or Approved (answered).</summary>
    public static async Task<IReadOnlyList<ContractorsReportVariation>> VariationsAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var rows = await context.VariationOrders.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.Status == (int)VariationOrderStatus.Issued)
            .OrderBy(row => row.Number)
            .ToListAsync(cancellationToken);
        return rows
            .Select(row => new ContractorsReportVariation(row.VariationOrderId, DisplayNumberOf(row), row.Title, DayOf(row.IssuedAt)))
            .ToList();
    }

    /// <summary>Section 4's opening line: the variations approved within the report's period.</summary>
    public static async Task<IReadOnlyList<ContractorsReportApprovedVariation>> ApprovedInPeriodAsync(
        JpmsContext context, string projectId, ReportingWeek week, CancellationToken cancellationToken)
    {
        var rows = await context.VariationOrders.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.Status == (int)VariationOrderStatus.Approved && row.ApprovedAt != null)
            .OrderBy(row => row.Number)
            .ToListAsync(cancellationToken);
        return rows
            .Select(row => new ContractorsReportApprovedVariation(row.VariationOrderId, DisplayNumberOf(row), DayOf(row.ApprovedAt) ?? DateOnly.MinValue))
            .Where(variation => week.Contains(variation.ApprovedOn))
            .ToList();
    }

    private static string DisplayNumberOf(VariationOrderEntity row) => row.Number > 0 ? $"V{row.Number}" : row.Reference;

    private static DateOnly? DayOf(DateTimeOffset? moment) => moment is { } value ? DateOnly.FromDateTime(value.Date) : null;

    /// <summary>Section 7: the standing contact on the project's active Building Control case, and the
    /// report's entered contact line for a project with none.</summary>
    public static async Task<ContractorsReportBuildingControl> BuildingControlAsync(
        JpmsContext context, ContractorsReport report, CancellationToken cancellationToken)
    {
        var activeCase = await context.BuildingControlCases.AsNoTracking()
            .Where(row => row.ProjectId == report.ProjectId && row.Status != (int)BuildingControlCaseStatus.Lapsed)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return new ContractorsReportBuildingControl(
            Blank(activeCase?.BodyName),
            Blank(activeCase?.ContactName),
            Blank(activeCase?.ContactEmail),
            Blank(activeCase?.ContactPhone),
            report.BuildingControlLiaison,
            Blank(report.BuildingControlContact));
    }

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
