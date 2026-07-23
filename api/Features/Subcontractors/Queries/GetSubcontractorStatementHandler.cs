using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

/// <summary>
/// Builds the subcontractor's statement of account from the register: their work orders across
/// every project, each with the Xero purchase invoices claimed against it (via the same
/// line-to-order links that drive the WO Allocation tab). Released and Complete orders always
/// appear; Draft and Cancelled orders only when invoices are already linked to them, so claimed
/// money never silently drops off the statement. Projects sort by reference, orders by number,
/// and a bill split across several orders carries only each order's share.
/// </summary>
public sealed class GetSubcontractorStatementHandler
    : IQueryHandler<GetSubcontractorStatement, SubcontractorStatement>
{
    private readonly JpmsContext context;

    public GetSubcontractorStatementHandler(JpmsContext context) { this.context = context; }

    public async Task<SubcontractorStatement> HandleAsync(
        GetSubcontractorStatement query, CancellationToken cancellationToken)
    {
        var subcontractor = await context.Subcontractors.FindAsync(
            new object[] { query.SubcontractorId }, cancellationToken);
        if (subcontractor is null)
            throw new InvalidOperationException($"Subcontractor {query.SubcontractorId} not found.");

        var orders = await context.WorkOrders
            .Where(order => order.SubcontractorId == query.SubcontractorId)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(order => order.WorkOrderId).ToList();
        var links = orderIds.Count == 0
            ? new List<XeroLineWorkOrderLinkEntity>()
            : await context.XeroLineWorkOrderLinks
                .Where(link => orderIds.Contains(link.WorkOrderId))
                .ToListAsync(cancellationToken);
        var linksByOrder = links
            .GroupBy(link => link.WorkOrderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var lineIds = links.Select(link => link.XeroLedgerLineId).Distinct().ToList();
        var ledgerLinesById = lineIds.Count == 0
            ? new Dictionary<string, XeroLedgerLineEntity>(StringComparer.OrdinalIgnoreCase)
            : await context.XeroLedgerLines
                .Where(line => lineIds.Contains(line.XeroLedgerLineId))
                .ToDictionaryAsync(line => line.XeroLedgerLineId,
                    StringComparer.OrdinalIgnoreCase, cancellationToken);

        // Released and Complete orders are the account; Draft and Cancelled orders only count once
        // money has actually been claimed against them.
        var statementOrders = orders
            .Where(order => (WorkOrderStatus)order.Status is WorkOrderStatus.Released or WorkOrderStatus.Complete
                || linksByOrder.ContainsKey(order.WorkOrderId))
            .ToList();

        var projectIds = statementOrders.Select(order => order.ProjectId).Distinct().ToList();
        var projectsById = projectIds.Count == 0
            ? new Dictionary<string, ProjectEntity>(StringComparer.OrdinalIgnoreCase)
            : await context.Projects
                .Where(project => projectIds.Contains(project.ProjectId))
                .ToDictionaryAsync(project => project.ProjectId,
                    StringComparer.OrdinalIgnoreCase, cancellationToken);

        var statementProjects = statementOrders
            .GroupBy(order => order.ProjectId, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                projectsById.TryGetValue(group.Key, out var project);
                var projectOrders = group
                    .OrderBy(order => order.Number)
                    .ThenBy(order => order.WorkOrderId, StringComparer.OrdinalIgnoreCase)
                    .Select(order => BuildOrder(order, linksByOrder, ledgerLinesById))
                    .ToList();
                return new SubcontractorStatementProject(
                    group.Key,
                    project?.Reference ?? "",
                    project?.Name ?? "(unknown project)",
                    projectOrders);
            })
            .OrderBy(project => project.ProjectReference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(project => project.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SubcontractorStatement(
            subcontractor.SubcontractorId,
            subcontractor.CompanyName,
            subcontractor.ContactName,
            subcontractor.ContactEmail,
            DateTimeOffset.UtcNow,
            statementProjects);
    }

    private static SubcontractorStatementOrder BuildOrder(
        WorkOrderEntity order,
        IReadOnlyDictionary<string, List<XeroLineWorkOrderLinkEntity>> linksByOrder,
        IReadOnlyDictionary<string, XeroLedgerLineEntity> ledgerLinesById)
    {
        linksByOrder.TryGetValue(order.WorkOrderId, out var orderLinks);

        // One statement row per Xero invoice: a bill whose several lines pay this order appears
        // once, with the signed sum of the slices this order carries.
        var invoices = (orderLinks ?? new List<XeroLineWorkOrderLinkEntity>())
            .Select(link => (Link: link,
                Line: ledgerLinesById.TryGetValue(link.XeroLedgerLineId, out var line) ? line : null))
            .GroupBy(entry => entry.Line?.XeroInvoiceId ?? entry.Link.XeroLedgerLineId,
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var line = group.Select(entry => entry.Line).FirstOrDefault(l => l is not null);
                return new SubcontractorStatementInvoice(
                    group.Key,
                    string.IsNullOrWhiteSpace(line?.InvoiceNumber) ? "(no invoice number)" : line!.InvoiceNumber!,
                    line?.Reference,
                    line?.Date,
                    line?.Type == "ACCPAYCREDIT",
                    group.Sum(entry => entry.Link.Amount));
            })
            .OrderBy(invoice => invoice.Date ?? DateTime.MaxValue)
            .ThenBy(invoice => invoice.InvoiceNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SubcontractorStatementOrder(
            order.WorkOrderId,
            order.Number,
            order.Title,
            (WorkOrderStatus)order.Status,
            order.AwardedAt,
            order.Value,
            invoices.Sum(invoice => invoice.Amount),
            invoices);
    }
}
