using System.Text.Json;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The tools the assistant can call, and the only ones it is ever told about.
///
/// <para>Filtered per user by <see cref="AiTool.VisibleTo"/> before the catalogue is sent, so a tool
/// the caller could not use is never described to the model — it cannot promise something it will
/// then be refused.</para>
///
/// <para>These read directly through EF rather than dispatching the CQRS query handlers. That is
/// acceptable only while the panel is gated to administrators and directors, who can already read
/// everything. The moment a narrower role gets the panel, each tool must route through its query
/// handler so the per-query role gate applies. Noted in docs/ai/00-agent-architecture.md §4.</para>
/// </summary>
public static class AiToolCatalogue
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>Every tool, before role filtering.</summary>
    public static IReadOnlyList<AiTool> All { get; } = Build();

    public static IReadOnlyList<AiTool> For(SignedInUser user) =>
        All.Where(tool => tool.VisibleTo.IncludesAny(user.Roles)).ToList();

    public static AiTool? Find(string name) =>
        All.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase));

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);

    private static string NotFound(string message) => Serialise(new { ok = false, error = message });

    private static IReadOnlyList<AiTool> Build()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "get_current_context",
                "What the user is currently looking at: the page, the project in view, who they are and today's date. "
                + "Call this first when the user says \"this project\", \"here\", or \"what am I looking at\".",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                readers,
                async (context, _, ct) =>
                {
                    var project = await ResolveProjectAsync(context, null, ct);
                    return Serialise(new
                    {
                        ok = true,
                        today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
                        user = new { context.User.Email, roles = context.User.Roles.Select(r => r.ToString()) },
                        page = context.Scope?.PageLabel,
                        route = context.Scope?.Route,
                        project = project is null
                            ? null
                            : new
                            {
                                project.ProjectId,
                                project.Reference,
                                project.Name,
                                stage = ((ProjectStage)project.Stage).ToString(),
                                client = project.ClientName
                            }
                    });
                }),

            new(
                "list_projects",
                "Every project that is not completed, with reference, name and stage. Use it to resolve a project "
                + "the user named in words rather than by reference.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                readers,
                async (context, _, ct) =>
                {
                    var projects = await context.Db.Projects
                        .AsNoTracking()
                        .Where(row => row.Stage != (int)ProjectStage.Completed)
                        .OrderBy(row => row.Reference)
                        .Select(row => new { row.ProjectId, row.Reference, row.Name, row.Stage })
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        projects = projects.Select(row => new
                        {
                            row.ProjectId,
                            row.Reference,
                            row.Name,
                            stage = ((ProjectStage)row.Stage).ToString()
                        })
                    });
                }),

            new(
                "get_project_contract",
                "The contract terms for a project: form and edition, contract sum, dates, LAD rate, retention, "
                + "the payment mechanism, and the overheads-and-profit and daywork percentages. "
                + "ALWAYS call this before quoting a clause, an OH&P percentage, a retention rate or a notice period — "
                + "these are contract terms and they differ per project. Returns ok:false when no contract is recorded.",
                AiToolSchema.Object(("projectId", "string", "Defaults to the project in view.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var project = await ResolveProjectAsync(context, AiToolSchema.Text(input, "projectId"), ct);
                    if (project is null) return NotFound("No project in scope. Ask the user which project, or call list_projects.");

                    var contract = await context.Db.ProjectContracts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row => row.ProjectId == project.ProjectId, ct);

                    if (contract is null)
                        return NotFound($"No contract has been recorded for {project.Reference}. Say so plainly — do not infer the terms.");

                    var form = (ContractForm)contract.Form;
                    return Serialise(new
                    {
                        ok = true,
                        project = project.Reference,
                        form = form.LongName(contract.FormEdition),
                        isAmended = form == ContractForm.Bespoke || !string.IsNullOrWhiteSpace(contract.BespokeDeviations),
                        contract.BespokeDeviations,
                        parties = new
                        {
                            employer = contract.EmployerName,
                            contractAdministrator = contract.ContractAdministratorName,
                            architect = contract.ArchitectName,
                            contractor = contract.ContractorName
                        },
                        contract.ContractSum,
                        contract.LiquidatedDamagesPerWeek,
                        dates = new { contract.ContractDate, contract.PossessionDate, contract.CompletionDate },
                        retention = new
                        {
                            beforeCompletionPercent = contract.RetentionPercent,
                            afterCompletionPercent = contract.RetentionPercentAfterCompletion,
                            contract.DefectsLiabilityPeriodMonths
                        },
                        payment = new
                        {
                            contract.ApplicationCutOffDayOfMonth,
                            contract.PaymentNoticeDays,
                            contract.PayLessNoticeDays,
                            contract.FinalDateForPaymentDays
                        },
                        ohp = new
                        {
                            directWorksPercent = contract.OhpDirectWorksPercent,
                            subcontractorPercent = contract.OhpSubcontractorPercent,
                            attendanceOnClientDirectPercent = contract.AttendanceOnClientDirectPercent,
                            dayworkLabourPercent = contract.DayworkLabourPercent,
                            dayworkMaterialsPercent = contract.DayworkMaterialsPercent,
                            dayworkPlantPercent = contract.DayworkPlantPercent
                        },
                        documentUploaded = !string.IsNullOrWhiteSpace(contract.DocumentFileName)
                    });
                }),

            new(
                "list_variations",
                "Variations on a project. A user always reads the number as V72 — never say VOQ or VO. "
                + "Status is one of Quoting, Issued, AwaitingArchitectInstruction (say \"Awaiting AI\"), Approved, Rejected.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("status", "string", "Optional filter: Quoting, Issued, AwaitingArchitectInstruction, Approved or Rejected.", false)),
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
                        count = rows.Count,
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
                + "Status is NeedsAction, Open, Closed or NeedsVariation.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("kind", "string", "Optional filter on the request kind.", false),
                    ("status", "string", "Optional filter on the request status.", false)),
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

                    var rows = await query
                        .OrderByDescending(row => row.RaisedAt)
                        .Take(100)
                        .Select(row => new
                        {
                            row.RequestId, row.Reference, row.Title, row.Kind, row.Status,
                            row.Value, row.RaisedAt, row.ResponseDue, row.ClosedAt, row.CriticalPath, row.RaisedTo
                        })
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        project = project.Reference,
                        count = rows.Count,
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
                            awaiting = row.RaisedTo,
                            route = $"/projects/{project.ProjectId}/requests/{row.RequestId}"
                        })
                    });
                }),

            new(
                "find_by_reference",
                "Look up a single record by the reference a person would say out loud — V72, RFI-049, REQ-0122, "
                + "NOD-003. Searches variations and requests across every project. Use this before saying you "
                + "cannot find something.",
                AiToolSchema.Object(("reference", "string", "For example V72 or RFI-049.", true)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var reference = AiToolSchema.Text(input, "reference")?.Trim();
                    if (string.IsNullOrWhiteSpace(reference)) return NotFound("A reference is required.");

                    // V72 — the number a user reads, which is VariationOrderEntity.Number per project.
                    if (reference.StartsWith("V", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(reference[1..], out var variationNumber))
                    {
                        var matches = await context.Db.VariationOrders
                            .AsNoTracking()
                            .Where(row => row.Number == variationNumber)
                            .Select(row => new
                            {
                                row.VariationOrderId, row.ProjectId, row.Number, row.Title,
                                row.Status, row.Value, row.VariationRef, row.RequestId, row.IssuedAt
                            })
                            .ToListAsync(ct);

                        if (matches.Count > 0)
                        {
                            var projectIds = matches.Select(row => row.ProjectId).ToList();
                            var projects = await context.Db.Projects
                                .AsNoTracking()
                                .Where(row => projectIds.Contains(row.ProjectId))
                                .ToDictionaryAsync(row => row.ProjectId, row => row.Reference, ct);

                            return Serialise(new
                            {
                                ok = true,
                                kind = "variation",
                                matches = matches.Select(row => new
                                {
                                    number = $"V{row.Number}",
                                    row.VariationOrderId,
                                    project = projects.TryGetValue(row.ProjectId, out var reference1) ? reference1 : row.ProjectId,
                                    row.Title,
                                    status = ((VariationOrderStatus)row.Status).ToString(),
                                    row.Value,
                                    row.RequestId,
                                    row.IssuedAt,
                                    route = $"/projects/{row.ProjectId}/variations/{row.VariationOrderId}"
                                })
                            });
                        }
                    }

                    var requests = await context.Db.Requests
                        .AsNoTracking()
                        .Where(row => row.Reference == reference)
                        .Select(row => new
                        {
                            row.RequestId, row.ProjectId, row.Reference, row.Title,
                            row.Kind, row.Status, row.Value, row.ResponseDue, row.RaisedTo
                        })
                        .ToListAsync(ct);

                    if (requests.Count == 0)
                        return NotFound($"Nothing found with reference {reference}. Say so — do not guess at a similar record.");

                    return Serialise(new
                    {
                        ok = true,
                        kind = "request",
                        matches = requests.Select(row => new
                        {
                            row.Reference,
                            row.RequestId,
                            row.Title,
                            kind = ((RequestType)row.Kind).ToString(),
                            status = ((RequestStatus)row.Status).ToString(),
                            row.Value,
                            row.ResponseDue,
                            awaiting = row.RaisedTo,
                            route = $"/projects/{row.ProjectId}/requests/{row.RequestId}"
                        })
                    });
                }),

            new(
                "navigate_to",
                "Take the user to a page in the portal. The page opens beside the chat. Use a route returned by "
                + "another tool. Say in one short clause where you are taking them and why.",
                AiToolSchema.Object(
                    ("route", "string", "A portal path, for example /projects/{id}/variations/{id}.", true),
                    ("reason", "string", "One clause explaining why.", false)),
                AiToolKind.Ui,
                JpmsRoleSets.AllInternal,
                // Never executed server-side — the handler returns it to the browser.
                (_, _, _) => Task.FromResult(Serialise(new { ok = true, navigated = true }))),
        };
    }

    /// <summary>The named project, else the one in scope, else null.</summary>
    private static async Task<Data.Entities.ProjectEntity?> ResolveProjectAsync(
        AiToolContext context, string? projectId, CancellationToken ct)
    {
        var id = string.IsNullOrWhiteSpace(projectId) ? context.Scope?.ProjectId : projectId;
        if (string.IsNullOrWhiteSpace(id)) return null;
        return await context.Db.Projects.AsNoTracking().FirstOrDefaultAsync(row => row.ProjectId == id, ct);
    }
}
