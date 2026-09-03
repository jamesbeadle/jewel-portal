using Ganss.Xss;
using Jewel.JPMS.Api.Features.Labour; // SiteClock (view_labour_week's week arithmetic)
using Jewel.JPMS.Api.Features.MailboxIntake.Graph; // IIntakeMessageReader (record email reads)
using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Xero; // GetXeroCostCodeOptionGaps (get_xero_cost_code_option_gaps)
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;


public static partial class AiToolCatalogue
{
    private static IEnumerable<AiTool> MastersTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "list_cost_codes",
                "The cost-centre master: every active cost code and the name against it. A scope line that goes "
                + "out to tender has to know which cost centre its committed value lands on. Call this before you "
                + "suggest a cost code on any line, and only ever use a Code returned here, spelled exactly as it "
                + "came back. If nothing clearly fits a line, leave its cost code out and let the user pick — a "
                + "wrong cost code sends real money to the wrong place and nobody notices for a month.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                // Mirrors ListCostCentersEndpoint.
                JpmsRoleSets.AllInternal,
                async (context, _, ct) =>
                {
                    var codes = await context.Db.CostCenters
                        .AsNoTracking()
                        .Where(row => row.IsActive)
                        .OrderBy(row => row.SortOrder).ThenBy(row => row.Code)
                        .Select(row => new { row.Code, row.Name })
                        .ToListAsync(ct);

                    return Serialise(new { ok = true, count = codes.Count, costCodes = codes });
                }),
            new(
                "get_xero_cost_code_option_gaps",
                "The drift between the portal's cost-code master and Xero's \"Cost Code\" tracking "
                + "category, read fresh from Xero: which active portal codes have NO option in Xero "
                + "(the coding run and bill approval create one lazily the first time a bill needs it, "
                + "so a code nobody has billed against never reaches Xero), which are archived there, "
                + "which are present, and which Xero options match no portal code (legacy numerics — "
                + "reported, never touched). Each code resolves to the option name it codes under: "
                + "its Xero mapping's tracking option when set, else the code itself. Also Xero's "
                + "active/archived option counts — Xero has historically capped active options per "
                + "category. The portal is deliberately NOT permitted to create or rename tracking "
                + "options in Xero (no settings write scope — a policy decision): this list is what a "
                + "person creates by hand in Xero under Settings → Tracking categories, and bill "
                + "approval / the coding run refuse a code whose option is missing until that is done.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                // Mirrors GetXeroCostCodeOptionGapsEndpoint / the Cost codes page's Xero tabs.
                RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator),
                async (context, _, ct) =>
                {
                    var gaps = await context.Services
                        .GetRequiredService<IQueryHandler<GetXeroCostCodeOptionGaps, XeroCostCodeOptionGaps>>()
                        .HandleAsync(new GetXeroCostCodeOptionGaps(), ct);
                    if (!gaps.IsConfigured) return NotFound("Xero isn't connected on this portal.");
                    if (gaps.Error is not null) return NotFound(gaps.Error);

                    return Serialise(new
                    {
                        ok = true,
                        xeroCategory = gaps.CategoryName,
                        activeOptionsInXero = gaps.ActiveOptionCount,
                        archivedOptionsInXero = gaps.ArchivedOptionCount,
                        missingCount = gaps.Missing.Count,
                        missing = gaps.Missing.Select(gap => new { gap.Code, gap.Name, optionName = gap.OptionName }),
                        archivedInXero = gaps.Archived.Select(gap => new { gap.Code, gap.Name, optionName = gap.OptionName }),
                        presentCount = gaps.Present.Count,
                        xeroOnlyOptions = gaps.XeroOnlyOptions,
                        note = "Create the missing options by hand in Xero (Settings → Tracking categories → "
                            + "the Cost Code category), spelt EXACTLY as optionName — the portal can't do it. "
                            + "Archived ones are restored there too. Xero-only options are legacy: leave them."
                    });
                }),
            new(
                "view_labour_week",
                "One project's labour week as the Labour tab shows it: every worker's timesheet days with "
                + "hours, cost code and status (Submitted / Approved / Rejected), plus a per-worker summary. "
                + "This is the view to show the user BEFORE coding or approving — code_worker_week and "
                + "approve_worker_week act on exactly what this returns. An uncoded day cannot be approved "
                + "until it is coded.",
                AiToolSchema.Object(
                    ("projectId", "string",
                        "The project, from list_projects. Left out, the project in scope is used.", false),
                    ("weekStart", "string",
                        "Any date in the week wanted, yyyy-MM-dd — it is normalised to that week's Monday. "
                        + "Left out, the current week.", false)),
                AiToolKind.Read,
                // Mirrors ListTimesheetDetailsForProjectEndpoint: all internal roles read hours; rates
                // and £ are stripped below unless the caller is on the commercial team.
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var project = await ResolveProjectAsync(context, AiToolSchema.Text(input, "projectId"), ct);
                    if (project is null)
                        return NotFound("Name a project — pass projectId from list_projects (or open a project page first).");

                    var anchorText = AiToolSchema.Text(input, "weekStart");
                    var anchor = !string.IsNullOrWhiteSpace(anchorText)
                                 && DateTimeOffset.TryParse(anchorText, out var parsed)
                        ? SiteClock.WorkDateOf(parsed)
                        : SiteClock.Today();
                    var weekStart = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
                    var weekEnd = weekStart.AddDays(7);

                    var rows = await context.Db.Timesheets.AsNoTracking()
                        .Where(row => row.ProjectId == project.ProjectId
                                      && row.WorkedOn >= weekStart && row.WorkedOn < weekEnd)
                        .OrderBy(row => row.WorkedOn)
                        .ToListAsync(ct);
                    var workerIds = rows.Select(row => row.WorkerId).Where(id => id != "").Distinct().ToList();
                    var names = await context.Db.Workers.AsNoTracking()
                        .Where(worker => workerIds.Contains(worker.WorkerId))
                        .ToDictionaryAsync(worker => worker.WorkerId, worker => worker.Name, ct);

                    // Same rule as the backing endpoint: hours for all internal roles, £ only for
                    // the commercial team.
                    var includeMoney = JpmsRoleSets.CommercialTeam.IncludesAny(context.User.Roles);

                    string NameOf(Data.Entities.TimesheetEntity row) =>
                        names.TryGetValue(row.WorkerId, out var found) ? found : row.PersonEmail;

                    var timesheets = rows.Select(row => new
                    {
                        worker = NameOf(row),
                        date = row.WorkedOn.ToString("yyyy-MM-dd"),
                        day = row.WorkedOn.ToString("ddd"),
                        hours = row.Hours,
                        costCode = string.IsNullOrWhiteSpace(row.CostCode) ? "uncoded" : row.CostCode,
                        status = ((TimesheetStatus)row.Status).ToString(),
                        rejectionReason = string.IsNullOrWhiteSpace(row.RejectionReason) ? null : row.RejectionReason,
                        approvedCost = includeMoney && row.Status == (int)TimesheetStatus.Approved
                            ? (decimal?)row.CostAmount
                            : null
                    }).ToList();

                    var workers = rows.GroupBy(NameOf).OrderBy(group => group.Key)
                        .Select(group => new
                        {
                            worker = group.Key,
                            days = group.Count(),
                            hours = group.Sum(row => row.Hours),
                            submitted = group.Count(row => row.Status == (int)TimesheetStatus.Submitted),
                            uncoded = group.Count(row => row.Status == (int)TimesheetStatus.Submitted
                                                         && string.IsNullOrWhiteSpace(row.CostCode)),
                            approved = group.Count(row => row.Status == (int)TimesheetStatus.Approved),
                            rejected = group.Count(row => row.Status == (int)TimesheetStatus.Rejected)
                        }).ToList();

                    return Serialise(new
                    {
                        ok = true,
                        project = new { project.ProjectId, project.Reference, project.Name },
                        weekStart = weekStart.ToString("yyyy-MM-dd"),
                        includesMoney = includeMoney,
                        workers,
                        timesheets,
                        note = rows.Count == 0
                            ? "No timesheets this week. Workers log time from their My day page, or a week is "
                              + "entered with submit_worker_week; submitted days then appear here for approval."
                            : "Only approved time posts to Financials as cost. Code Submitted days with "
                              + "code_worker_week (uncoded days cannot approve), then approve with "
                              + "approve_worker_week — which is confirm-first: show the user these days and "
                              + "get their yes."
                    });
                }),
        };
    }
}
