using System.Text.Json;
using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The valuation loop's readers (docs/ai/06-context-retrieval.md, Phase 2): one variation in full
/// — header, linked request, the approved lines that stand on the Valuation Report under its
/// V-number with their claimed % — and the live Valuation Report itself, line by line, with the
/// selected claim's % complete and the previous claim's. Together with the two dialogs
/// (variation_edit_lines, claim_progress) they close "update V01 to the V01 tab and correct its
/// % complete": read the tab, read the variation, read the report, fill the dialogs, the user
/// presses Save.
///
/// <para>Every line carries its ValuationLineItemId because that is what the dialogs key on —
/// descriptions repeat, ids do not.</para>
/// </summary>
internal static class AiCommercialTools
{
    public const string GetVariationContext = "get_variation_context";
    public const string GetValuationContext = "get_valuation_context";
    public const string GetCostCodeBudgets = "get_cost_code_budgets";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    /// <summary>The same tolerance find_by_reference gives a variation: V72, VO72, VOQ-0072, v 72.</summary>
    private static readonly Regex VariationReference = new("^v(?:oq|o)?0*(\\d+)$", RegexOptions.Compiled);

    /// <summary>Lines returned per call before the result says it clipped — a whole report is a
    /// few hundred lines at most; the cap is a safety net, not a page size.</summary>
    private const int MaxLines = 600;

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                GetVariationContext,
                "One variation in full: its header (number, title, status, estimated and approved "
                + "value, narratives, dates), the request or RFI behind it, the priced lines that stand "
                + "on the Valuation Report under its V-number — each with its valuationLineItemId, cost "
                + "centre, quantity, rate and amount, and its % complete on the latest claim — the "
                + "cost-centre split, and the work orders raised to instruct it. Resolves \"V01\", "
                + "\"VO 80\" or \"VOQ-0080\" on the project in view (pass projectId elsewhere), or a "
                + "variationOrderId. Call this BEFORE comparing a variation with a spreadsheet or "
                + "opening variation_edit_lines; the lines here are exactly what that dialog edits.",
                AiToolSchema.Object(
                    ("reference", "string", "The number as the user says it — V01, V72, VO 80.", false),
                    ("variationOrderId", "string", "The variation's id, when you already have it (find_by_reference, list_variations).", false),
                    ("projectId", "string", "Defaults to the project in view. Needed with a reference when no project is in view.", false)),
                AiToolKind.Read,
                JpmsRoleSets.InternalAndArchitect,
                async (context, input, ct) =>
                {
                    var id = AiToolSchema.Text(input, "variationOrderId");
                    var reference = AiToolSchema.Text(input, "reference");
                    var projectId = AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;

                    if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(reference)
                        && string.Equals(context.Scope?.RecordType, "variation", StringComparison.OrdinalIgnoreCase))
                        id = context.Scope?.RecordId; // The variation on the page in view.

                    if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(reference))
                        return Fail("Say which variation: pass reference (V01) or variationOrderId, or have the user open the variation's page.");

                    VariationOrderEntity? order = null;
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        order = await context.Db.VariationOrders.AsNoTracking()
                            .FirstOrDefaultAsync(row => row.VariationOrderId == id, ct);
                        if (order is null) return Fail($"No variation exists with id \"{id}\".");
                    }
                    else
                    {
                        var cleaned = reference!.Replace("-", "").Replace(" ", "").ToLowerInvariant();
                        var match = VariationReference.Match(cleaned);
                        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var number))
                            return Fail($"\"{reference}\" is not a variation number — they read V01, V72, VO 80.");

                        var candidates = await context.Db.VariationOrders.AsNoTracking()
                            .Where(row => row.Number == number && (projectId == null || row.ProjectId == projectId))
                            .ToListAsync(ct);
                        if (candidates.Count == 0)
                        {
                            return Fail(projectId is null
                                ? $"No variation numbered V{number} exists on any project."
                                : $"No variation numbered V{number} exists on this project — list_variations shows what does.");
                        }
                        if (candidates.Count > 1)
                        {
                            var projects = await ProjectReferences(context, candidates.Select(row => row.ProjectId), ct);
                            return Fail($"V{number} exists on more than one project: "
                                + string.Join("; ", candidates.Select(row =>
                                    $"{(projects.TryGetValue(row.ProjectId, out var p) ? p : row.ProjectId)} (variationOrderId {row.VariationOrderId})"))
                                + ". Pass projectId or variationOrderId.");
                        }
                        order = candidates[0];
                    }

                    var project = await context.Db.Projects.AsNoTracking()
                        .Where(row => row.ProjectId == order.ProjectId)
                        .Select(row => new { row.Reference, row.Name })
                        .FirstOrDefaultAsync(ct);

                    var requestRow = string.IsNullOrWhiteSpace(order.RequestId)
                        ? null
                        : await context.Db.Requests.AsNoTracking()
                            .FirstOrDefaultAsync(row => row.RequestId == order.RequestId, ct);
                    var request = requestRow is null
                        ? null
                        : new
                        {
                            requestRow.RequestId,
                            reference = requestRow.Reference,
                            kind = ((RequestType)requestRow.Kind).ToString(),
                            requestRow.Title,
                            status = ((RequestStatus)requestRow.Status).ToString(),
                            requestRow.Value,
                            requestRow.RespondedAt,
                            response = requestRow.ResponseText,
                            route = $"/projects/{requestRow.ProjectId}/requests/view/{requestRow.RequestId}"
                        };

                    // The approved build-up IS the report's Variation lines under the V-ref —
                    // there is no separate lines table. Empty until approval.
                    var lines = string.IsNullOrWhiteSpace(order.VariationRef)
                        ? new List<ValuationLineItemEntity>()
                        : await context.Db.ValuationLineItems.AsNoTracking()
                            .Where(row => row.ProjectId == order.ProjectId
                                          && row.ElementType == (int)ValuationElementType.Variation
                                          && row.VariationRef == order.VariationRef)
                            .OrderBy(row => row.DisplayOrder).ThenBy(row => row.ValuationLineItemId)
                            .ToListAsync(ct);

                    // % complete on the latest claim, so "is it claimed yet" is one call.
                    var latestClaim = await context.Db.ValuationClaims.AsNoTracking()
                        .Where(row => row.ProjectId == order.ProjectId)
                        .OrderByDescending(row => row.ClaimNumber)
                        .Select(row => new { row.ValuationClaimId, row.ClaimNumber, row.Name, row.Status })
                        .FirstOrDefaultAsync(ct);
                    var lineIds = lines.Select(row => row.ValuationLineItemId).ToList();
                    var entries = latestClaim is null || lineIds.Count == 0
                        ? new Dictionary<string, decimal>()
                        : await context.Db.ClaimLines.AsNoTracking()
                            .Where(row => row.ValuationClaimId == latestClaim.ValuationClaimId && lineIds.Contains(row.ValuationLineItemId))
                            .ToDictionaryAsync(row => row.ValuationLineItemId, row => row.PercentComplete, ct);

                    var workOrders = (await context.Db.WorkOrders.AsNoTracking()
                            .Where(row => row.VariationOrderId == order.VariationOrderId)
                            .OrderBy(row => row.Number)
                            .ToListAsync(ct))
                        .Select(row => new
                        {
                            row.WorkOrderId,
                            reference = row.Number > 0 ? $"WO-{row.Number:0000}" : "(draft)",
                            row.Title,
                            status = ((WorkOrderStatus)row.Status).ToString(),
                            row.Value,
                            route = $"/projects/{row.ProjectId}/work-orders?record={row.WorkOrderId}"
                        })
                        .ToList();

                    var status = (VariationOrderStatus)order.Status;
                    var displayNumber = order.Number > 0 ? $"V{order.Number}" : order.Reference;
                    return Serialise(new
                    {
                        ok = true,
                        variation = new
                        {
                            number = displayNumber,
                            order.VariationOrderId,
                            project = project is null ? order.ProjectId : $"{project.Reference} — {project.Name}",
                            order.ProjectId,
                            order.Title,
                            order.Description,
                            status = status.ToString(),
                            statusMeaning = status.Hint(),
                            estimatedValue = order.EstimatedValue,
                            approvedValue = status == VariationOrderStatus.Approved ? order.Value : (decimal?)null,
                            approvedRef = order.VariationRef,
                            primaryCostCode = order.CostCode,
                            created = order.CreatedAt,
                            issued = order.IssuedAt,
                            approved = order.ApprovedAt,
                            rejected = order.RejectedAt,
                            commercialBasis = order.CommercialBasis,
                            programmeImpact = order.ProgrammeImpact,
                            exclusions = order.Exclusions,
                            route = $"/projects/{order.ProjectId}/variations/{order.VariationOrderId}"
                        },
                        request,
                        lines = lines.Select(row => new
                        {
                            row.ValuationLineItemId,
                            row.CostCode,
                            row.Description,
                            lineType = ((ValuationLineType)row.LineType).ToString(),
                            row.Unit,
                            row.Quantity,
                            row.Rate,
                            row.LineAmount,
                            percentCompleteOnLatestClaim = entries.TryGetValue(row.ValuationLineItemId, out var percent) ? percent : (decimal?)null
                        }).ToList(),
                        linesTotal = lines.Sum(row => row.LineAmount),
                        draftLines = Variations.VariationDraftLines.Parse(order.DraftLinesJson)?
                            .Select(line => new { line.CostCode, line.Description, line.Quantity, line.Rate, amount = line.Quantity * line.Rate })
                            .ToList(),
                        costCentres = lines.GroupBy(row => row.CostCode)
                            .Select(group => new { costCode = group.Key, amount = group.Sum(row => row.LineAmount) })
                            .ToList(),
                        latestClaim = latestClaim is null
                            ? null
                            : new
                            {
                                latestClaim.ValuationClaimId,
                                name = string.IsNullOrWhiteSpace(latestClaim.Name) ? $"Claim {latestClaim.ClaimNumber}" : latestClaim.Name,
                                status = ((ValuationClaimStatus)latestClaim.Status).ToString()
                            },
                        workOrders,
                        note = status == VariationOrderStatus.Approved
                            ? "To change the priced lines, open_modal \"variation_edit_lines\" with this variationOrderId "
                              + "as record_id — send the whole schedule back, keeping each kept line's "
                              + "valuationLineItemId. To change the % complete, get_valuation_context then "
                              + "open_modal \"claim_progress\" on the Valuation Report. Neither writes until the "
                              + "user presses Save."
                            : status == VariationOrderStatus.Rejected
                                ? "Rejected — closed; nothing stands on the Valuation Report and nothing can be staged."
                                : "Not yet approved, so no lines stand on the Valuation Report. To stage the client-agreed "
                                  + "build-up (lines and narratives), open_modal \"variation_build_up\" with this "
                                  + "variationOrderId as record_id and send the whole schedule; the user presses Stage "
                                  + "build-up, the total becomes the estimate, and the approve modal opens pre-seeded. "
                                  + "The staged lines, if any, are under draftLines."
                    });
                }),

            new(
                GetValuationContext,
                "The project's LIVE Valuation Report, line by line: every line's valuationLineItemId, "
                + "section (contract works, PC sums, contingency, variations — with the V-number and "
                + "title on variation lines), cost centre, description, quantity, rate and amount, its "
                + "cumulative % complete and £ claimed on the SELECTED claim, the previous claim's %, "
                + "and this period's increment — plus the claims list (which is selected, its status) "
                + "and the report totals. The selected claim is the newest unless claimId says "
                + "otherwise. Filter with variationRef (\"V01\") to see one variation's lines only. "
                + "Call this before reviewing or correcting % complete, and before claim_progress.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("claimId", "string", "A claim's id from an earlier call. Defaults to the newest claim.", false),
                    ("variationRef", "string", "Only the lines of this variation — V01, V72.", false),
                    ("section", "string",
                        "Only one section: contract_works, pc_sums, contingency or variations.", false)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var projectId = AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids) or have the user open one of its pages.");

                    var project = await context.Db.Projects.AsNoTracking()
                        .Where(row => row.ProjectId == projectId)
                        .Select(row => new { row.ProjectId, row.Reference, row.Name })
                        .FirstOrDefaultAsync(ct);
                    if (project is null) return Fail($"No project exists with id \"{projectId}\".");

                    var claims = await context.Db.ValuationClaims.AsNoTracking()
                        .Where(row => row.ProjectId == projectId)
                        .OrderByDescending(row => row.ClaimNumber)
                        .ToListAsync(ct);

                    var requestedClaimId = AiToolSchema.Text(input, "claimId");
                    var selected = string.IsNullOrWhiteSpace(requestedClaimId)
                        ? claims.FirstOrDefault()
                        : claims.FirstOrDefault(row => row.ValuationClaimId == requestedClaimId);
                    if (!string.IsNullOrWhiteSpace(requestedClaimId) && selected is null)
                        return Fail($"No claim with id \"{requestedClaimId}\" exists on this project — the claims are listed in a call without claimId.");

                    // "Previous" as the report table shows it: the claim immediately before the
                    // selected one by number, whatever its status.
                    var previous = selected is null
                        ? null
                        : claims.Where(row => row.ClaimNumber < selected.ClaimNumber)
                            .OrderByDescending(row => row.ClaimNumber)
                            .FirstOrDefault();

                    var allLines = await context.Db.ValuationLineItems.AsNoTracking()
                        .Where(row => row.ProjectId == projectId)
                        .OrderBy(row => row.ElementType).ThenBy(row => row.DisplayOrder)
                        .ToListAsync(ct);

                    var claimIds = new List<string>();
                    if (selected is not null) claimIds.Add(selected.ValuationClaimId);
                    if (previous is not null) claimIds.Add(previous.ValuationClaimId);
                    var entries = claimIds.Count == 0
                        ? new List<ClaimLineEntity>()
                        : await context.Db.ClaimLines.AsNoTracking()
                            .Where(row => claimIds.Contains(row.ValuationClaimId))
                            .ToListAsync(ct);
                    var selectedEntries = selected is null
                        ? new Dictionary<string, ClaimLineEntity>()
                        : entries.Where(row => row.ValuationClaimId == selected.ValuationClaimId)
                            .ToDictionary(row => row.ValuationLineItemId);
                    var previousEntries = previous is null
                        ? new Dictionary<string, ClaimLineEntity>()
                        : entries.Where(row => row.ValuationClaimId == previous.ValuationClaimId)
                            .ToDictionary(row => row.ValuationLineItemId);

                    // Filters: one variation, or one section.
                    var variationRef = NormaliseVariationRef(AiToolSchema.Text(input, "variationRef"));
                    var sectionFilter = AiToolSchema.Text(input, "section")?.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
                    ValuationElementType? elementFilter = sectionFilter switch
                    {
                        "contract_works" or "contract" or "works" => ValuationElementType.ContractWorks,
                        "pc_sums" or "pc" or "pcsum" or "pc_sum" => ValuationElementType.PcSum,
                        "contingency" => ValuationElementType.Contingency,
                        "variations" or "variation" => ValuationElementType.Variation,
                        null or "" => null,
                        _ => null
                    };
                    if (!string.IsNullOrWhiteSpace(sectionFilter) && elementFilter is null)
                        return Fail($"\"{sectionFilter}\" is not a section — use contract_works, pc_sums, contingency or variations.");

                    var filtered = allLines
                        .Where(row => elementFilter is null || row.ElementType == (int)elementFilter)
                        .Where(row => variationRef is null
                                      || (row.ElementType == (int)ValuationElementType.Variation
                                          && string.Equals(NormaliseVariationRef(row.VariationRef), variationRef, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var shown = filtered.Take(MaxLines).ToList();
                    var lineRows = shown.Select(row =>
                    {
                        selectedEntries.TryGetValue(row.ValuationLineItemId, out var now);
                        previousEntries.TryGetValue(row.ValuationLineItemId, out var before);
                        var counts = (ValuationLineType)row.LineType is not (ValuationLineType.Declined or ValuationLineType.Tbc);
                        return new
                        {
                            row.ValuationLineItemId,
                            section = SectionName((ValuationElementType)row.ElementType),
                            area = row.ElementType == (int)ValuationElementType.Variation ? null : row.SectionName,
                            variationRef = row.ElementType == (int)ValuationElementType.Variation ? row.VariationRef : null,
                            variationTitle = row.ElementType == (int)ValuationElementType.Variation ? row.VariationTitle : null,
                            row.CostCode,
                            row.Description,
                            lineType = ((ValuationLineType)row.LineType).ToString(),
                            countsTowardTotals = counts,
                            row.Unit,
                            row.Quantity,
                            row.Rate,
                            row.LineAmount,
                            percentComplete = counts ? (decimal?)(now?.PercentComplete ?? 0m) : null,
                            claimedToDate = counts ? (decimal?)(now?.CumulativeClaimed ?? 0m) : null,
                            previousPercent = counts && previous is not null ? (decimal?)(before?.PercentComplete ?? 0m) : null,
                            periodIncrement = counts ? (decimal?)(now?.PeriodIncrement ?? 0m) : null
                        };
                    }).ToList();

                    // Totals over the WHOLE report (not the filter), so a filtered read still says
                    // where the report stands.
                    var counting = allLines.Where(row => (ValuationLineType)row.LineType is not (ValuationLineType.Declined or ValuationLineType.Tbc)).ToList();
                    var contractSum = counting.Where(row => row.ElementType != (int)ValuationElementType.Variation).Sum(row => row.LineAmount);
                    var netVariations = counting.Where(row => row.ElementType == (int)ValuationElementType.Variation).Sum(row => row.LineAmount);
                    var worksComplete = counting.Sum(row => selectedEntries.TryGetValue(row.ValuationLineItemId, out var entry) ? entry.CumulativeClaimed : 0m);

                    return Serialise(new
                    {
                        ok = true,
                        project = $"{project.Reference} — {project.Name}",
                        project.ProjectId,
                        claims = claims.Select(row => new
                        {
                            row.ValuationClaimId,
                            number = row.ClaimNumber,
                            name = string.IsNullOrWhiteSpace(row.Name) ? $"Claim {row.ClaimNumber}" : row.Name,
                            date = row.ClaimDate,
                            status = ((ValuationClaimStatus)row.Status).ToString(),
                            selected = selected is not null && row.ValuationClaimId == selected.ValuationClaimId
                        }).ToList(),
                        selectedClaim = selected is null
                            ? null
                            : new
                            {
                                selected.ValuationClaimId,
                                name = string.IsNullOrWhiteSpace(selected.Name) ? $"Claim {selected.ClaimNumber}" : selected.Name,
                                status = ((ValuationClaimStatus)selected.Status).ToString(),
                                editable = selected.Status == (int)ValuationClaimStatus.Draft,
                                previousClaim = previous is null ? null : (string.IsNullOrWhiteSpace(previous.Name) ? $"Claim {previous.ClaimNumber}" : previous.Name)
                            },
                        totals = new
                        {
                            contractSum,
                            netVariations,
                            revisedContractSum = contractSum + netVariations,
                            totalWorksCompleteOnSelectedClaim = worksComplete
                        },
                        filter = new { variationRef, section = elementFilter?.ToString() },
                        lineCount = filtered.Count,
                        lines = lineRows,
                        clipped = filtered.Count > shown.Count,
                        route = $"/projects/{projectId}/valuation",
                        note = (selected is null
                                   ? "No claim exists yet — % complete can only be recorded on a Draft claim; the user starts one on the Valuation Report tab. "
                                   : selected.Status == (int)ValuationClaimStatus.Draft
                                       ? "The selected claim is Draft, so % complete can be changed: open_modal \"claim_progress\" with the lines to change (valuationLineItemId + cumulative percentComplete). "
                                       : $"The selected claim is {((ValuationClaimStatus)selected.Status)}, so its % complete is locked — say so; a new Draft claim is started on the Valuation Report tab. ")
                               + "Percentages are CUMULATIVE to date. Every figure here is read from the report; quote it, never estimate."
                               + (filtered.Count > shown.Count ? $" Only the first {MaxLines} of {filtered.Count} lines are shown — filter by section or variationRef." : "")
                    });
                }),

            new(
                GetCostCodeBudgets,
                "The project's cost code budgets as the Financials tab holds them: each code's "
                + "allocated, spent and committed amounts, the approved labour cost standing "
                + "against it, and the remaining budget the labour hard-block tests "
                + "(allocated − spent − committed − approved labour). Call this BEFORE "
                + "set_cost_code_budget — that action takes ABSOLUTE figures, so the new "
                + "allocation is computed from the current one read here, never guessed — and "
                + "after any budget hard-block refusal, to show the user the code's standing "
                + "position. Codes carrying approved labour with no budget row are listed too "
                + "(they block labour approval outright).",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("costCode", "string", "Only this code's row — a Code from list_cost_codes.", false)),
                AiToolKind.Read,
                // The Financials tab's own audience: mirrors FinancialsTabManagers in
                // CommercialActions (who may change budgets and cost completion), which also
                // covers everyone LabourRoleSets.ApproveTimesheets lets approve into them.
                RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
                    JpmsRoles.ProjectManager, JpmsRoles.Estimator),
                async (context, input, ct) =>
                {
                    var projectId = AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids) or have the user open one of its pages.");

                    var project = await context.Db.Projects.AsNoTracking()
                        .Where(row => row.ProjectId == projectId)
                        .Select(row => new { row.ProjectId, row.Reference, row.Name })
                        .FirstOrDefaultAsync(ct);
                    if (project is null) return Fail($"No project exists with id \"{projectId}\".");

                    var codeFilter = AiToolSchema.Text(input, "costCode")?.Trim();

                    var budgets = await context.Db.CostCodeBudgets.AsNoTracking()
                        .Where(row => row.ProjectId == projectId)
                        .OrderBy(row => row.CostCode)
                        .ToListAsync(ct);

                    // Approved labour per code — the same figure the hard-block counts against
                    // remaining budget (ApproveTimesheetsHandler), so the numbers here and a
                    // refusal message can never disagree.
                    var approvedLabour = (await context.Db.Timesheets.AsNoTracking()
                            .Where(row => row.ProjectId == projectId && row.Status == (int)TimesheetStatus.Approved)
                            .GroupBy(row => row.CostCode)
                            .Select(group => new { CostCode = group.Key, Amount = group.Sum(row => row.CostAmount) })
                            .ToListAsync(ct))
                        .ToDictionary(row => row.CostCode, row => row.Amount, StringComparer.OrdinalIgnoreCase);

                    var names = (await context.Db.CostCenters.AsNoTracking()
                            .Select(row => new { row.Code, row.Name })
                            .ToListAsync(ct))
                        .ToDictionary(row => row.Code, row => row.Name, StringComparer.OrdinalIgnoreCase);

                    var rows = budgets
                        .Where(row => codeFilter is null || string.Equals(row.CostCode, codeFilter, StringComparison.OrdinalIgnoreCase))
                        .Select(row =>
                        {
                            var labour = approvedLabour.TryGetValue(row.CostCode, out var sum) ? sum : 0m;
                            return new
                            {
                                costCode = row.CostCode,
                                name = names.TryGetValue(row.CostCode, out var name) ? name : null,
                                hasBudgetRow = true,
                                allocatedAmount = row.AllocatedAmount,
                                spentAmount = row.SpentAmount,
                                committedAmount = row.CommittedAmount,
                                approvedLabourToDate = labour,
                                remainingBudget = row.AllocatedAmount - row.SpentAmount - row.CommittedAmount - labour
                            };
                        })
                        .ToList();

                    // Codes with labour cost but NO budget row: invisible on the Financials tab's
                    // budget list, yet they refuse every labour approval — surfaced so \u0022no budget
                    // is set\u0022 refusals have somewhere to point.
                    var unbudgeted = approvedLabour.Keys
                        .Where(code => !string.IsNullOrWhiteSpace(code)
                                       && budgets.All(row => !string.Equals(row.CostCode, code, StringComparison.OrdinalIgnoreCase))
                                       && (codeFilter is null || string.Equals(code, codeFilter, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(code => code)
                        .Select(code => new
                        {
                            costCode = code,
                            name = names.TryGetValue(code, out var name) ? name : null,
                            hasBudgetRow = false,
                            allocatedAmount = 0m,
                            spentAmount = 0m,
                            committedAmount = 0m,
                            approvedLabourToDate = approvedLabour[code],
                            remainingBudget = -approvedLabour[code]
                        })
                        .ToList();

                    if (codeFilter is not null && rows.Count == 0 && unbudgeted.Count == 0)
                        return Fail($"No budget row or approved labour exists for cost code \"{codeFilter}\" on this project — a call without costCode lists what does.");

                    return Serialise(new
                    {
                        ok = true,
                        project = $"{project.Reference} — {project.Name}",
                        project.ProjectId,
                        budgets = rows.Concat(unbudgeted).ToList(),
                        note = "remainingBudget = allocated − spent − committed − approved labour — "
                               + "exactly what the labour approval hard-block tests. Figures are read from the "
                               + "Financials tab's rows; quote them, never estimate. Changing a budget is "
                               + "set_cost_code_budget (confirm-first, absolute figures, audited)."
                    });
                })
        };
    }

    private static string SectionName(ValuationElementType elementType) => elementType switch
    {
        ValuationElementType.ContractWorks => "Contract works",
        ValuationElementType.PcSum => "PC sums",
        ValuationElementType.Contingency => "Contingency",
        ValuationElementType.Variation => "Variations",
        _ => elementType.ToString()
    };

    /// <summary>"V01", "v1", "VO 1" and "V001" all mean the same line: normalised to "V1".</summary>
    private static string? NormaliseVariationRef(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Replace("-", "").Replace(" ", "").ToLowerInvariant();
        var match = VariationReference.Match(cleaned);
        return match.Success && int.TryParse(match.Groups[1].Value, out var number)
            ? $"V{number}"
            : value.Trim().ToUpperInvariant();
    }

    private static async Task<Dictionary<string, string>> ProjectReferences(AiToolContext context, IEnumerable<string> projectIds, CancellationToken ct)
    {
        var ids = projectIds.Distinct().ToList();
        return await context.Db.Projects.AsNoTracking()
            .Where(row => ids.Contains(row.ProjectId))
            .ToDictionaryAsync(row => row.ProjectId, row => row.Reference, ct);
    }
}
