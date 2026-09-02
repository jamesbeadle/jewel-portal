using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiCommercialTools
{
    private static IEnumerable<AiTool> CostCodeBudgetsTool()
    {
        return new AiTool[]
        {
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

                    // Grouped rather than keyed one-to-one: retired seed generations left the
                    // master with rows sharing a code, and a duplicated code must never turn
                    // the budget read into a 500. The active row's name wins.
                    var names = (await context.Db.CostCenters.AsNoTracking()
                            .Select(row => new { row.Code, row.Name, row.IsActive })
                            .ToListAsync(ct))
                        .GroupBy(row => row.Code, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            group => group.Key,
                            group => group.OrderByDescending(row => row.IsActive).First().Name,
                            StringComparer.OrdinalIgnoreCase);

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
}
