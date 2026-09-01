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
    private static IEnumerable<AiTool> ContextTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "get_current_context",
                "Who you are acting as — the signed-in portal user, their roles — and today's date. "
                + "Call this first when unsure what the user may see or do.",
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
                "Every live project with its id, reference, name and stage. This is how you resolve a project "
                + "the user named in words (\"By France\") or by reference (JBB-2026-001) to the id a route or "
                + "a dialog needs — call it BEFORE navigating to another project's pages; the id goes in the "
                + "route in place of {project}. Completed (handed-over) projects are left out unless you pass "
                + "include_completed: true, which is what to do when a name matches nothing.",
                AiToolSchema.Object(
                    ("include_completed", "boolean",
                        "true adds completed projects to the list — pass it when the user names a project "
                        + "that has been handed over, or when the name they used matched nothing.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var includeCompleted = AiToolSchema.Flag(input, "include_completed") ?? false;
                    var projects = await context.Db.Projects
                        .AsNoTracking()
                        .Where(row => includeCompleted || row.Stage != (int)ProjectStage.Completed)
                        .OrderBy(row => row.Reference)
                        .Select(row => new { row.ProjectId, row.Reference, row.Name, row.Stage })
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        includes_completed = includeCompleted,
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
                + "the payment mechanism, the overheads-and-profit and daywork percentages, and any recorded "
                + "amendments (deeds of variation, side letters) in date order. "
                + "ALWAYS call this before quoting a clause, an OH&P percentage, a retention rate or a notice period — "
                + "these are contract terms and they differ per project, and an amendment may have moved them since "
                + "the contract was signed. Returns ok:false when no contract is recorded.",
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

                    // The amendments register, in the order the amendments were made. The terms
                    // above are already the current position — the register says how it got there,
                    // and its notes are the first place to look when a figure surprises.
                    var amendmentRows = await context.Db.ProjectContractAmendments
                        .AsNoTracking()
                        .Where(row => row.ProjectId == project.ProjectId)
                        .ToListAsync(ct);

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
                        documentUploaded = !string.IsNullOrWhiteSpace(contract.DocumentFileName),
                        amendments = amendmentRows
                            .OrderBy(row => row.AmendmentDate ?? row.DocumentUploadedAt)
                            .ThenBy(row => row.DocumentUploadedAt)
                            .Select(row => new
                            {
                                row.Title,
                                row.AmendmentDate,
                                row.Notes,
                                uploadedAt = row.DocumentUploadedAt
                            })
                            .ToList()
                    });
                }),
        };
    }
}
