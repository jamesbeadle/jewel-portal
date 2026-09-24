using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>
/// "Instructions and confirmations received this period" at the end of Section 1 (Jeremy's review
/// of Report 31, 24 Sep 2026), read from the registers rather than lifted out of the day notes:
/// the architect's instructions received, the variations approved, and the RFIs raised or closed
/// within the Friday-to-Thursday week, oldest first. An RFI still at Needs action is in-house and
/// is not printed. Empty when nothing happened — the block is then left out.
/// </summary>
internal static class ContractorsReportInstructions
{
    public static async Task<IReadOnlyList<string>> ReceivedAsync(
        JpmsContext context, string projectId, ReportingWeek week, CancellationToken cancellationToken)
    {
        var dated = new List<(DateOnly Day, string Line)>();
        dated.AddRange(await InstructionsAsync(context, projectId, week, cancellationToken));
        dated.AddRange(await ApprovalsAsync(context, projectId, week, cancellationToken));
        dated.AddRange(await RfisAsync(context, projectId, week, cancellationToken));
        return dated.OrderBy(item => item.Day).Select(item => item.Line).ToList();
    }

    private static async Task<IEnumerable<(DateOnly, string)>> InstructionsAsync(
        JpmsContext context, string projectId, ReportingWeek week, CancellationToken cancellationToken)
    {
        var rows = await context.ArchitectInstructions.AsNoTracking()
            .Where(row => row.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        return rows
            .Select(row => (Day: DayOf(row.ReceivedAt), Row: row))
            .Where(item => week.Contains(item.Day))
            .Select(item => (item.Day, $"{ReferenceOf(item.Row.InstructionRef, item.Row.Reference)} — {item.Row.Title}, received on {ContractorsReportPrintedText.LongDate(item.Day)}"));
    }

    private static async Task<IEnumerable<(DateOnly, string)>> ApprovalsAsync(
        JpmsContext context, string projectId, ReportingWeek week, CancellationToken cancellationToken)
    {
        var approved = await ContractorsReportRegisterReader.ApprovedInPeriodAsync(context, projectId, week, cancellationToken);
        var titles = await context.VariationOrders.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.Status == (int)VariationOrderStatus.Approved)
            .ToDictionaryAsync(row => row.VariationOrderId, row => row.Title, cancellationToken);
        return approved.Select(variation => (variation.ApprovedOn,
            $"{variation.DisplayNumber} — {titles.GetValueOrDefault(variation.VariationOrderId, "")}, approved on {ContractorsReportPrintedText.LongDate(variation.ApprovedOn)}"));
    }

    private static async Task<IEnumerable<(DateOnly, string)>> RfisAsync(
        JpmsContext context, string projectId, ReportingWeek week, CancellationToken cancellationToken)
    {
        var rows = await context.Requests.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.Kind == (int)RequestType.Rfi && row.Status != (int)RequestStatus.NeedsAction)
            .ToListAsync(cancellationToken);
        var raised = rows
            .Select(row => (Day: DayOf(row.IssuedAt ?? row.RaisedAt), Row: row))
            .Where(item => week.Contains(item.Day))
            .Select(item => (item.Day, $"{item.Row.Title} ({item.Row.Reference}) raised on {ContractorsReportPrintedText.LongDate(item.Day)}"));
        var closed = rows
            .Where(row => row.ClosedAt is not null)
            .Select(row => (Day: DayOf(row.ClosedAt!.Value), Row: row))
            .Where(item => week.Contains(item.Day))
            .Select(item => (item.Day, $"{item.Row.Title} ({item.Row.Reference}) answered and closed on {ContractorsReportPrintedText.LongDate(item.Day)}"));
        return raised.Concat(closed);
    }

    private static string ReferenceOf(string instructionRef, string reference) =>
        string.IsNullOrWhiteSpace(instructionRef) ? reference : instructionRef.Trim();

    private static DateOnly DayOf(DateTimeOffset moment) => DateOnly.FromDateTime(moment.Date);
}
