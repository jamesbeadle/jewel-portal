using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>Section 8: the work orders live on site — Released, or completed within the week —
/// with the supplier named from the directory and the attendance the report's author entered.</summary>
internal static class ContractorsReportSubcontractorsReader
{
    public static async Task<IReadOnlyList<ContractorsReportSubcontractor>> ReadAsync(
        JpmsContext context,
        string projectId,
        ReportingWeek week,
        IReadOnlyList<ContractorsReportAttendance> attendance,
        CancellationToken cancellationToken)
    {
        var orders = await context.WorkOrders.AsNoTracking()
            .Where(row => row.ProjectId == projectId
                && (row.Status == (int)WorkOrderStatus.Released || row.Status == (int)WorkOrderStatus.Complete))
            .OrderBy(row => row.Number)
            .ToListAsync(cancellationToken);
        var live = orders.Where(order => IsOnSite(order, week)).ToList();

        var supplierIds = live.Select(order => order.SubcontractorId).Distinct().ToList();
        var suppliers = await context.Subcontractors.AsNoTracking()
            .Where(row => supplierIds.Contains(row.SubcontractorId))
            .ToDictionaryAsync(row => row.SubcontractorId, row => row.CompanyName, cancellationToken);
        var entered = attendance.ToDictionary(item => item.WorkOrderId);

        return live
            .Select(order => new ContractorsReportSubcontractor(
                order.WorkOrderId,
                order.Reference,
                suppliers.TryGetValue(order.SubcontractorId, out var name) ? name : "",
                string.IsNullOrWhiteSpace(order.Title) ? order.Scope : order.Title,
                order.Value,
                order.ScheduledCompletion is { } completion ? DateOnly.FromDateTime(completion.Date) : null,
                entered.TryGetValue(order.WorkOrderId, out var days) ? days.AttendanceDays : null,
                entered.TryGetValue(order.WorkOrderId, out var nominated) && nominated.IsClientNominated))
            .ToList();
    }

    private static bool IsOnSite(WorkOrderEntity order, ReportingWeek week) =>
        order.Status == (int)WorkOrderStatus.Released
        || (order.ScheduledCompletion is { } completion && week.Contains(DateOnly.FromDateTime(completion.Date)));
}
