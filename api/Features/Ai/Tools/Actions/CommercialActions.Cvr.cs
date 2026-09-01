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
    private static IEnumerable<AiAction> CvrActions() => new AiAction[]
    {
        // ── CVR ──────────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "capture_cvr_snapshot",
            Area: "CVR",
            Description: "Captures a CVR (cost value reconciliation) snapshot for a project — tender "
                + "value, forecast final cost, forecast final value and weeks ahead/behind — the "
                + "period's recorded view of forecast profit.",
            CommandType: typeof(CaptureCvrSnapshot),
            ResultType: typeof(CvrSnapshot),
            AuthorisationType: typeof(CaptureCvrSnapshotAuthorisation),
            ValidationType: typeof(CaptureCvrSnapshotValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the figures with the user before calling — a snapshot is a period record. "
                + "projectId comes from list_projects."),

        new AiAction(
            Name: "record_cvr_package_row",
            Area: "CVR",
            Description: "Records a package row on a project's CVR — order cost/value and variation "
                + "cost/value for one named package, feeding the CVR's cost-versus-value position.",
            CommandType: typeof(RecordCvrPackageRow),
            ResultType: typeof(CvrPackageRow),
            AuthorisationType: typeof(RecordCvrPackageRowAuthorisation),
            ValidationType: typeof(RecordCvrPackageRowValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_forecast_component",
            Area: "CVR",
            Description: "Records a cost-forecast component for one package on a project's CVR — cost "
                + "incurred, committed, QS accrual, prelim forecast and cost to complete — the build-up "
                + "behind the forecast final cost.",
            CommandType: typeof(RecordForecastComponent),
            ResultType: typeof(ForecastComponent),
            AuthorisationType: typeof(RecordForecastComponentAuthorisation),
            ValidationType: typeof(RecordForecastComponentValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_prelim_forecast_for_week",
            Area: "CVR",
            Description: "Records one week's prelim position for a prelim item on a project — tendered, "
                + "actual and forecast amounts — feeding the prelims run-rate in the CVR.",
            CommandType: typeof(RecordPrelimForecastForWeek),
            ResultType: typeof(PrelimForecastEntry),
            AuthorisationType: typeof(RecordPrelimForecastForWeekAuthorisation),
            ValidationType: typeof(RecordPrelimForecastForWeekValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "record_qs_accrual",
            Area: "CVR",
            Description: "Records a QS accrual on a project — add/omit amounts and the liability "
                + "carried in the CVR for cost known but not yet invoiced, signed off by a named "
                + "person.",
            CommandType: typeof(RecordQsAccrual),
            ResultType: typeof(QsAccrual),
            AuthorisationType: typeof(RecordQsAccrualAuthorisation),
            ValidationType: typeof(RecordQsAccrualValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "signedOffByEmail is the accountable signer's portal email — pass it explicitly; it "
                + "is not stamped from the signed-in user. projectId comes from list_projects."),

        new AiAction(
            Name: "update_qs_accrual",
            Area: "CVR",
            Description: "Rewrites an existing QS accrual's details — category, description, add/omit "
                + "amounts, liability and signer — changing the accrued liability the CVR carries.",
            CommandType: typeof(UpdateQsAccrual),
            ResultType: typeof(QsAccrual),
            AuthorisationType: typeof(UpdateQsAccrualAuthorisation),
            ValidationType: typeof(UpdateQsAccrualValidation),
            VisibleTo: CvrEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This replaces every field on the accrual — read the current record first and carry "
                + "forward what should not change. qsAccrualId comes from the project's QS accruals "
                + "list."),

        new AiAction(
            Name: "grant_eot",
            Area: "CVR",
            Description: "Grants an extension of time (EOT) on a project — days granted with the "
                + "commercial recovery amount attached. A contractual/commercial commitment; directors "
                + "only.",
            CommandType: typeof(GrantEot),
            ResultType: typeof(Eot),
            AuthorisationType: typeof(GrantEotAuthorisation),
            ValidationType: typeof(GrantEotValidation),
            VisibleTo: DirectorsOnly,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — granting an EOT is a formal commitment. "
                + "projectId comes from list_projects."),

        new AiAction(
            Name: "update_eot",
            Area: "CVR",
            Description: "Rewrites a granted EOT's reason, days granted and commercial recovery amount "
                + "— changing a formal commitment already on record. Directors only.",
            CommandType: typeof(UpdateEot),
            ResultType: typeof(Eot),
            AuthorisationType: typeof(UpdateEotAuthorisation),
            ValidationType: typeof(UpdateEotValidation),
            VisibleTo: DirectorsOnly,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. eotId comes from the project's EOTs list; "
                + "this replaces all three fields, so carry forward what should not change."),

    };
}
