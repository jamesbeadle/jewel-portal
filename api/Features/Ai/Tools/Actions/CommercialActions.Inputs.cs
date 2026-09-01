using Jewel.JPMS.Api.Features.Cashflow.Commands;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Api.Features.CommercialInputs.Commands;
using Jewel.JPMS.Api.Features.Cvr.Commands;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class CommercialActions
{
    private static IEnumerable<AiAction> InputsActions() => new AiAction[]
    {
        // ── Commercial inputs: dayworks, contra charges, subcontractor retentions ────────

        new AiAction(
            Name: "log_daywork",
            Area: "Commercial",
            Description: "Logs a daywork on a project — labour, plant and materials costs with uplift, "
                + "producing the chargeable amount recorded against the subcontractor reference. Adds a "
                + "cost/recovery record the commercial team reports from.",
            CommandType: typeof(LogDaywork),
            ResultType: typeof(Daywork),
            AuthorisationType: typeof(LogDayworkAuthorisation),
            ValidationType: typeof(LogDayworkValidation),
            VisibleTo: DayworkLoggers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_contra_charge",
            Area: "Commercial",
            Description: "Records a contra charge against a subcontractor — an amount to be recovered "
                + "from them (with category, status and recovered-to-date) that the commercial team "
                + "offsets against what the subcontractor is owed.",
            CommandType: typeof(RecordContraCharge),
            ResultType: typeof(ContraCharge),
            AuthorisationType: typeof(RecordContraChargeAuthorisation),
            ValidationType: typeof(RecordContraChargeValidation),
            VisibleTo: ContraChargeRecorders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_subcontractor_retention",
            Area: "Commercial",
            Description: "Records a subcontractor's retention position on a project — certified amount, "
                + "retention percent and the first/final released amounts — the money held back from "
                + "the subcontractor and what has been released.",
            CommandType: typeof(RecordSubcontractorRetention),
            ResultType: typeof(SubcontractorRetention),
            AuthorisationType: typeof(RecordSubcontractorRetentionAuthorisation),
            ValidationType: typeof(RecordSubcontractorRetentionValidation),
            VisibleTo: RetentionRecorders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

    };
}
