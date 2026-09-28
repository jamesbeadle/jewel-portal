using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>The work orders live on site — Released, or completed within the week — with the
/// supplier named from the directory, the attendance the report's author entered, and the days
/// the firm's own workers signed in on the site register. These are what attendance is entered
/// against; Section 8 prints the ones that attended.</summary>
internal static class ContractorsReportSubcontractorsReader
{
    public static async Task<IReadOnlyList<ContractorsReportSubcontractor>> ReadAsync(
        JpmsContext context,
        string projectId,
        ReportingWeek week,
        IReadOnlyList<ContractorsReportAttendance> attendance,
        IReadOnlyList<ContractorsReportSignIn> signIns,
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
        var projectReference = await WorkOrderProjectReferences.OfAsync(context, projectId, cancellationToken);

        return live
            .Select(order => new ContractorsReportSubcontractor(
                order.WorkOrderId,
                order.ReferenceOn(projectReference),
                suppliers.TryGetValue(order.SubcontractorId, out var name) ? name : "",
                ScopeFor(entered.GetValueOrDefault(order.WorkOrderId), TitleOf(order)),
                TitleOf(order),
                order.Value,
                order.ScheduledCompletion is { } completion ? DateOnly.FromDateTime(completion.Date) : null,
                AttendanceDaysOf(entered.GetValueOrDefault(order.WorkOrderId)),
                entered.TryGetValue(order.WorkOrderId, out var nominated) && nominated.IsClientNominated,
                DaysOnSiteOf(entered.GetValueOrDefault(order.WorkOrderId)),
                DaysSignedInBy(order.SubcontractorId, signIns)))
            .ToList();
    }

    /// <summary>The firm's days on site off the register: every day one of its workers signed in.</summary>
    public static IReadOnlyList<DateOnly> DaysSignedInBy(string subcontractorId, IReadOnlyList<ContractorsReportSignIn> signIns) =>
        signIns
            .Where(signIn => signIn.SubcontractorId == subcontractorId)
            .Select(signIn => signIn.Date)
            .Distinct()
            .Order()
            .ToList();

    /// <summary>The attendance a new report opens with: the sign-ins ticked onto a firm's one live
    /// order. A firm with several live orders is left to the person — the register says the firm
    /// was on site, not which order the day belongs to.</summary>
    public static IReadOnlyList<ContractorsReportAttendance> FromSignIns(IReadOnlyList<ContractorsReportSubcontractor> live) =>
        live
            .Where(order => order.DaysSignedIn.Any())
            .GroupBy(order => order.Supplier)
            .Where(firm => firm.Count() == 1)
            .Select(firm => firm.Single())
            .Select(order => new ContractorsReportAttendance(order.WorkOrderId, order.DaysSignedIn.Count, false, "", order.DaysSignedIn))
            .ToList();

    private static IReadOnlyList<DateOnly> DaysOnSiteOf(ContractorsReportAttendance? attendance) =>
        attendance?.DaysOnSite ?? Array.Empty<DateOnly>();

    private static int? AttendanceDaysOf(ContractorsReportAttendance? attendance)
    {
        var ticked = DaysOnSiteOf(attendance).Count;
        return ticked > 0 ? ticked : attendance?.AttendanceDays;
    }

    private static string TitleOf(WorkOrderEntity order) => string.IsNullOrWhiteSpace(order.Title) ? order.Scope : order.Title;

    private static string ScopeFor(ContractorsReportAttendance? attendance, string title) =>
        attendance is { Scope: { } scope } && !string.IsNullOrWhiteSpace(scope) ? scope.Trim() : title;

    private static bool IsOnSite(WorkOrderEntity order, ReportingWeek week) =>
        order.Status == (int)WorkOrderStatus.Released
        || (order.ScheduledCompletion is { } completion && week.Contains(DateOnly.FromDateTime(completion.Date)));
}
