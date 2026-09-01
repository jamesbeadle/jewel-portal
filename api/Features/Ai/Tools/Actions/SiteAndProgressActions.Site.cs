using Jewel.JPMS.Api.Features.Closeout.Commands;
using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SiteAndProgressActions
{
    private static IEnumerable<AiAction> SiteActions() => new AiAction[]
    {
        new AiAction(
            Name: "assemble_site_report",
            Area: "Site",
            Description: "Creates a new site report for a project (period end, narrative, "
                + "attendance days, open snags, progress percent). The report starts un-issued — "
                + "approve_site_report issues it.",
            CommandType: typeof(AssembleSiteReport),
            ResultType: typeof(SiteReport),
            AuthorisationType: typeof(AssembleSiteReportAuthorisation),
            ValidationType: typeof(AssembleSiteReportValidation),
            VisibleTo: SiteTeamManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Dates are ISO 8601."),

        new AiAction(
            Name: "approve_site_report",
            Area: "Site",
            Description: "Approves (issues) a site report — marks it issued for the project "
                + "record. This is a sign-off.",
            CommandType: typeof(ApproveSiteReport),
            ResultType: typeof(SiteReport),
            AuthorisationType: typeof(ApproveSiteReportAuthorisation),
            ValidationType: typeof(ApproveSiteReportValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which report, by project and period, before calling."),

        // ── Progress & programme (programme of works) ─────────────────────────────────────────

    };
}
