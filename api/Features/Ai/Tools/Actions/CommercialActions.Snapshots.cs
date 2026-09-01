using Jewel.JPMS.Api.Features.Cashflow.Commands;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Api.Features.CommercialInputs.Commands;
using Jewel.JPMS.Api.Features.Cvr.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class CommercialActions
{
    private static IEnumerable<AiAction> SnapshotsActions() => new AiAction[]
    {
        // ── Commercial: valuation report snapshots ───────────────────────────────────────

        new AiAction(
            Name: "take_valuation_report_snapshot",
            Area: "Commercial",
            Description: "Freezes an immutable, line-level copy of the project's valuation report as it "
                + "stands right now — every priced line with % complete and cumulative claimed, plus the "
                + "summary and retention footer with certified-to-date stamped at this moment. The "
                + "period-end financial record; an amendment means taking a NEW snapshot.",
            CommandType: typeof(TakeValuationReportSnapshot),
            ResultType: typeof(ValuationReportSnapshot),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(TakeValuationReportSnapshotValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling, and agree the label. Leave "
                + "valuationInvoiceId null — it is set by the automatic capture behind an invoice "
                + "submission, not on-demand snapshots."),

        new AiAction(
            Name: "delete_valuation_report_snapshot",
            Area: "Commercial",
            Description: "Permanently removes a valuation report snapshot taken in error, with its "
                + "lines. Never touches live report data; any invoice pointing at it has its snapshot "
                + "link cleared. There is no undo.",
            CommandType: typeof(DeleteValuationReportSnapshot),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which snapshot, by label and date, before calling."),

        new AiAction(
            Name: "prepare_valuation_report_snapshot_email_draft",
            Area: "Commercial",
            Description: "Creates a DRAFT email in the shared mailbox addressed to the project's Client "
                + "and Architect contacts, with the frozen valuation report attached as a PDF — nothing "
                + "is sent; a human reviews and sends it from Outlook. The subject and HTML cover note "
                + "are supplied by the caller.",
            CommandType: typeof(PrepareValuationReportSnapshotEmailDraft),
            ResultType: typeof(ValuationReportSnapshotEmailDraft),
            AuthorisationType: typeof(PrepareValuationReportSnapshotEmailDraftAuthorisation),
            ValidationType: typeof(PrepareValuationReportSnapshotEmailDraftValidation),
            VisibleTo: SnapshotEmailDrafters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This is client-facing money correspondence — confirm the subject and cover-note "
                + "wording with the user before calling. Recipients are fixed to the project's Client "
                + "and Architect contacts (projects@ is cc'd automatically). valuationReportSnapshotId "
                + "comes from the project's snapshots list. The result's draftMessageId is the handle "
                + "for delete_mailbox_draft if the draft has to be withdrawn."),

    };
}
