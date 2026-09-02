using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiCommercialTools
{
    private static async Task<Dictionary<string, string>> ProjectReferences(AiToolContext context, IEnumerable<string> projectIds, CancellationToken ct)
    {
        var ids = projectIds.Distinct().ToList();
        return await context.Db.Projects.AsNoTracking()
            .Where(row => ids.Contains(row.ProjectId))
            .ToDictionaryAsync(row => row.ProjectId, row => row.Reference, ct);
    }

    private static IEnumerable<AiTool> VariationContextTool()
    {
        return new AiTool[]
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
                })
        };
    }
}
