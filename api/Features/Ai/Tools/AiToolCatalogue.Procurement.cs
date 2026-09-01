using Ganss.Xss;
using Jewel.JPMS.Api.Features.Labour; // SiteClock (view_labour_week's week arithmetic)
using Jewel.JPMS.Api.Features.MailboxIntake.Graph; // IIntakeMessageReader (record email reads)
using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;


public static partial class AiToolCatalogue
{
    private static IEnumerable<AiTool> ProcurementTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "list_work_orders",
                "Work orders on a project — DRAFTS INCLUDED. Status is Draft, Released, Complete, Cancelled "
                + "or Rejected. A draft has NO order number yet (the number is minted at approval), so a "
                + "draft can only be found here — find_by_reference cannot see it. \"Edit the tiling draft "
                + "on this page\" → call this (status Draft), take the workOrderId, then get_work_order_context "
                + "and open_modal work_order_edit. Looking for an order by trade, supplier or what it covers? "
                + "Pass search on the FIRST call — do not page the register blind.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("status", "string", "Optional filter: Draft, Released, Complete, Cancelled or Rejected.", false),
                    ("search", "string",
                        "Text matched against order titles, scopes and supplier names — \"tiling\", \"Sussex\". "
                        + "Use it to find the order the user described instead of reading the whole register.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var project = await ResolveProjectAsync(context, AiToolSchema.Text(input, "projectId"), ct);
                    if (project is null) return NotFound("No project in scope. Ask the user which project, or call list_projects.");

                    var query = context.Db.WorkOrders
                        .AsNoTracking()
                        .Where(row => row.ProjectId == project.ProjectId);

                    var statusText = AiToolSchema.Text(input, "status");
                    if (!string.IsNullOrWhiteSpace(statusText)
                        && Enum.TryParse<WorkOrderStatus>(statusText, ignoreCase: true, out var status))
                    {
                        query = query.Where(row => row.Status == (int)status);
                    }

                    var orderSearch = AiToolSchema.Text(input, "search")?.Trim();
                    if (!string.IsNullOrWhiteSpace(orderSearch))
                    {
                        // The supplier's name lives on Subcontractors, not the order row — resolve the
                        // matching ids first so "Sussex" finds the order that only stores the id.
                        var supplierIds = await context.Db.Subcontractors.AsNoTracking()
                            .Where(row => row.CompanyName.Contains(orderSearch))
                            .Select(row => row.SubcontractorId)
                            .ToListAsync(ct);
                        query = query.Where(row =>
                            row.Title.Contains(orderSearch)
                            || row.Scope.Contains(orderSearch)
                            || supplierIds.Contains(row.SubcontractorId));
                    }

                    var orderTotal = await query.CountAsync(ct);

                    var orders = await query
                        .OrderByDescending(row => row.CreatedAt)
                        .Take(100)
                        .ToListAsync(ct);

                    var orderSupplierIds = orders.Select(row => row.SubcontractorId).Distinct().ToList();
                    var supplierNames = await context.Db.Subcontractors.AsNoTracking()
                        .Where(row => orderSupplierIds.Contains(row.SubcontractorId))
                        .ToDictionaryAsync(row => row.SubcontractorId, row => row.CompanyName, ct);

                    return Serialise(new
                    {
                        ok = true,
                        project = project.Reference,
                        projectId = project.ProjectId,
                        count = orders.Count,
                        totalMatching = orderTotal,
                        // The cap said out loud — a silently clipped register reads as "not found".
                        note = orderTotal > orders.Count
                            ? $"Only the newest {orders.Count} of {orderTotal} matching orders are listed. "
                              + "Pass search to narrow instead of calling again blind."
                            : "get_work_order_context (workOrderId) reads an order's lines and attachments; "
                              + "open_modal work_order_edit (record_id = workOrderId) edits it — drafts included.",
                        workOrders = orders.Select(row => new
                        {
                            row.WorkOrderId,
                            // A draft's computed Reference is an id stem, not a number a person knows —
                            // null says "no number yet" instead of teaching the model a fake reference.
                            reference = row.Number > 0 ? row.Reference : null,
                            status = ((WorkOrderStatus)row.Status).ToString(),
                            row.Title,
                            supplier = supplierNames.TryGetValue(row.SubcontractorId, out var name) ? name : row.SubcontractorId,
                            value = row.Value,
                            createdAt = row.CreatedAt,
                            targetCompletion = row.ScheduledCompletion,
                            route = $"/projects/{project.ProjectId}/work-orders"
                        })
                    });
                }),
            new(
                "list_bid_packages",
                "Bid packages on a project — the tendering records, one per trade scope. Status is Draft, "
                + "Inviting, QuotesReceived, Awarded or Closed. Looking for a package by trade or title? "
                + "Pass search on the FIRST call — do not page the register blind.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("status", "string", "Optional filter: Draft, Inviting, QuotesReceived, Awarded or Closed.", false),
                    ("search", "string",
                        "Text matched against package titles and trades — \"tiling\", \"groundworks\".", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var project = await ResolveProjectAsync(context, AiToolSchema.Text(input, "projectId"), ct);
                    if (project is null) return NotFound("No project in scope. Ask the user which project, or call list_projects.");

                    var query = context.Db.BidPackages
                        .AsNoTracking()
                        .Where(row => row.ProjectId == project.ProjectId);

                    var statusText = AiToolSchema.Text(input, "status");
                    if (!string.IsNullOrWhiteSpace(statusText)
                        && Enum.TryParse<BidPackageStatus>(statusText, ignoreCase: true, out var status))
                    {
                        query = query.Where(row => row.Status == (int)status);
                    }

                    var packageSearch = AiToolSchema.Text(input, "search")?.Trim();
                    if (!string.IsNullOrWhiteSpace(packageSearch))
                        query = query.Where(row => row.Title.Contains(packageSearch) || row.Trade.Contains(packageSearch));

                    var packageTotal = await query.CountAsync(ct);

                    var packages = await query
                        .OrderByDescending(row => row.CreatedAt)
                        .Take(100)
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        project = project.Reference,
                        projectId = project.ProjectId,
                        count = packages.Count,
                        totalMatching = packageTotal,
                        note = packageTotal > packages.Count
                            ? $"Only the newest {packages.Count} of {packageTotal} matching packages are listed. "
                              + "Pass search to narrow instead of calling again blind."
                            : "get_bid_package_context (bidPackageId) reads a package's detail and invites; "
                              + "read_record_emails record_type bid_package reads its tender correspondence.",
                        bidPackages = packages.Select(row => new
                        {
                            row.BidPackageId,
                            reference = row.Number > 0 ? row.Reference : null,
                            status = ((BidPackageStatus)row.Status).ToString(),
                            row.Title,
                            row.Trade,
                            owner = row.OwnerEmail,
                            createdAt = row.CreatedAt,
                            route = $"/projects/{project.ProjectId}/bid-package-invites/{row.BidPackageId}"
                        })
                    });
                }),
        };
    }
}
