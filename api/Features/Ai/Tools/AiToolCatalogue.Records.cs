using System.Text.Json;
using Ganss.Xss;
using Jewel.JPMS.Api.Features.Labour; // SiteClock (view_labour_week's week arithmetic)
using Jewel.JPMS.Api.Features.MailboxIntake.Graph; // IIntakeMessageReader (record email reads)
using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;


public static partial class AiToolCatalogue
{
    private static IEnumerable<AiTool> RecordsTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "list_variations",
                "Variations on a project. A user always reads the number as V72 — never say VOQ or VO. "
                + "Status is one of Quoting, Issued, AwaitingArchitectInstruction (say \"Awaiting AI\"), Approved, Rejected. "
                + "Looking for a variation by what it is about? Pass search — do not page through the register.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("status", "string", "Optional filter: Quoting, Issued, AwaitingArchitectInstruction, Approved or Rejected.", false),
                    ("search", "string",
                        "Text matched against the variation titles — \"render\", \"front door\". Use it to find "
                        + "the variation the user described instead of reading the whole book.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var project = await ResolveProjectAsync(context, AiToolSchema.Text(input, "projectId"), ct);
                    if (project is null) return NotFound("No project in scope. Ask the user which project, or call list_projects.");

                    var query = context.Db.VariationOrders
                        .AsNoTracking()
                        .Where(row => row.ProjectId == project.ProjectId);

                    var statusText = AiToolSchema.Text(input, "status");
                    if (!string.IsNullOrWhiteSpace(statusText)
                        && Enum.TryParse<VariationOrderStatus>(statusText, ignoreCase: true, out var status))
                    {
                        query = query.Where(row => row.Status == (int)status);
                    }

                    var variationSearch = AiToolSchema.Text(input, "search")?.Trim();
                    if (!string.IsNullOrWhiteSpace(variationSearch))
                        query = query.Where(row => row.Title.Contains(variationSearch));

                    var variationTotal = await query.CountAsync(ct);

                    var rows = await query
                        .OrderByDescending(row => row.Number)
                        .Take(100)
                        .Select(row => new
                        {
                            row.VariationOrderId, row.Number, row.Title, row.Status,
                            row.Value, row.VariationRef, row.RequestId, row.IssuedAt, row.ApprovedAt
                        })
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        project = project.Reference,
                        projectId = project.ProjectId,
                        count = rows.Count,
                        totalMatching = variationTotal,
                        // The cap said out loud — a silently clipped register reads as "not found".
                        note = variationTotal > rows.Count
                            ? $"Only the highest-numbered {rows.Count} of {variationTotal} matching variations are "
                              + "listed. Pass search to narrow instead of calling again blind."
                            : null,
                        variations = rows.Select(row => new
                        {
                            number = $"V{row.Number}",
                            row.VariationOrderId,
                            row.Title,
                            status = ((VariationOrderStatus)row.Status).ToString(),
                            row.Value,
                            approvedRef = row.VariationRef,
                            row.RequestId,
                            row.IssuedAt,
                            row.ApprovedAt,
                            route = $"/projects/{project.ProjectId}/variations/{row.VariationOrderId}"
                        })
                    });
                }),
            new(
                "list_requests",
                "Requests on a project. The lineage is Request → RFI → Variation, one document with one number "
                + "through every stage. Kind is Rfi, Rfa, Rfc, NoticeOfDelay, Rfq, Rfp, ExtensionOfTime or General. "
                + "Status is NeedsAction, Open, Closed or NeedsVariation. "
                + "Looking for a request by what it is about (\"the front door RFI\")? Pass search on the FIRST "
                + "call — the register can be longer than one page, and paging it blind wastes your look-ups.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("kind", "string", "Optional filter on the request kind.", false),
                    ("status", "string", "Optional filter on the request status.", false),
                    ("search", "string",
                        "Text matched against the request titles and references — \"front door\", \"render\", "
                        + "\"REQ-0113\". Use it to find the request the user described instead of reading the "
                        + "whole register.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var project = await ResolveProjectAsync(context, AiToolSchema.Text(input, "projectId"), ct);
                    if (project is null) return NotFound("No project in scope. Ask the user which project, or call list_projects.");

                    var query = context.Db.Requests
                        .AsNoTracking()
                        .Where(row => row.ProjectId == project.ProjectId);

                    var kindText = AiToolSchema.Text(input, "kind");
                    if (!string.IsNullOrWhiteSpace(kindText)
                        && Enum.TryParse<RequestType>(kindText, ignoreCase: true, out var kind))
                    {
                        query = query.Where(row => row.Kind == (int)kind);
                    }

                    var statusText = AiToolSchema.Text(input, "status");
                    if (!string.IsNullOrWhiteSpace(statusText)
                        && Enum.TryParse<RequestStatus>(statusText, ignoreCase: true, out var status))
                    {
                        query = query.Where(row => row.Status == (int)status);
                    }

                    var requestSearch = AiToolSchema.Text(input, "search")?.Trim();
                    if (!string.IsNullOrWhiteSpace(requestSearch))
                        query = query.Where(row => row.Title.Contains(requestSearch) || row.Reference.Contains(requestSearch));

                    var requestTotal = await query.CountAsync(ct);

                    var rows = await query
                        .OrderByDescending(row => row.RaisedAt)
                        .Take(100)
                        .Select(row => new
                        {
                            row.RequestId, row.Reference, row.Title, row.Kind, row.Status,
                            row.Value, row.RaisedAt, row.ResponseDue, row.ClosedAt, row.CriticalPath
                        })
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        project = project.Reference,
                        projectId = project.ProjectId,
                        count = rows.Count,
                        totalMatching = requestTotal,
                        // The cap said out loud — a silently clipped register reads as "not found",
                        // and the model then pages the register blind, one look-up at a time.
                        note = requestTotal > rows.Count
                            ? $"Only the newest {rows.Count} of {requestTotal} matching requests are listed. Pass "
                              + "search to narrow to the one you want instead of calling again blind."
                            : null,
                        requests = rows.Select(row => new
                        {
                            row.Reference,
                            row.RequestId,
                            row.Title,
                            kind = ((RequestType)row.Kind).ToString(),
                            status = ((RequestStatus)row.Status).ToString(),
                            row.Value,
                            row.RaisedAt,
                            row.ResponseDue,
                            row.ClosedAt,
                            row.CriticalPath,
                            // The detail page is /requests/view/{id} (ProjectRequestDetail.razor).
                            // Without "view" this lands on the request LIST with the id read as a
                            // kind filter — a navigate_to that silently went to the wrong page.
                            route = $"/projects/{project.ProjectId}/requests/view/{row.RequestId}"
                        })
                    });
                }),
        };
    }
}
