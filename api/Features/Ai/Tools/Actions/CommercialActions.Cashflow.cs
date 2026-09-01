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
    private static IEnumerable<AiAction> CashflowActions() => new AiAction[]
    {
        // ── Cashflow ─────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "capture_cashflow_snapshot",
            Area: "Cashflow",
            Description: "Captures a company-wide 13-week cashflow snapshot — expected income and "
                + "committed spend — recording the net cash position the directors report from. Not "
                + "per-project.",
            CommandType: typeof(CaptureCashflowSnapshot),
            ResultType: typeof(CashflowSnapshot),
            AuthorisationType: typeof(CaptureCashflowSnapshotAuthorisation),
            ValidationType: typeof(CaptureCashflowSnapshotValidation),
            VisibleTo: CashflowCapturers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the figures with the user before calling — a snapshot is a standing "
                + "financial record.")
    };
}
