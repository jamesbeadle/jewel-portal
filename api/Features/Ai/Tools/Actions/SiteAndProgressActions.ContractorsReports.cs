using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SiteAndProgressActions
{
    private const string ContractorsReportArea = "Progress & programme";

    private static IEnumerable<AiAction> ContractorsReportActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_contractors_report",
            Area: ContractorsReportArea,
            Description: "Opens the weekly Contractor's Report for the week ending on a Thursday, "
                + "pre-filled with what a person would otherwise copy from last week: the next "
                + "number, the programme reference / prepared by / issued to carried forward, the "
                + "previous report's unstruck Look Ahead items, the highest payment certificate "
                + "on the register as the Valuation No., the Neighbours default line, and every "
                + "progress update in the period selected. Returns the record; read it back with "
                + "get_contractors_report to see the composed document.",
            CommandType: typeof(CreateContractorsReport),
            ResultType: typeof(ContractorsReport),
            AuthorisationType: typeof(CreateContractorsReportAuthorisation),
            ValidationType: typeof(CreateContractorsReportValidation),
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; periodEnd is the Thursday the week ends on "
                + "(yyyy-MM-dd). Leave number blank to take the next in sequence. A second report "
                + "for the same period is refused — list_contractors_reports shows what exists."),

        new AiAction(
            Name: "update_contractors_report",
            Area: ContractorsReportArea,
            Description: "Writes the ENTERED fields of a Contractor's Report: Valuation No., "
                + "programme reference, prepared by, issued to, date of issue, the Look Ahead "
                + "items (text + isDone — a done item is struck and not carried to next week), "
                + "Neighbours, Health & Safety, the Building Control liaison line, the Section 8 "
                + "attendance per work order (workOrderId, attendanceDays, isClientNominated) and "
                + "which progress updates are selected. Everything else in the document is read "
                + "from the register at build time and cannot be edited here.",
            CommandType: typeof(UpdateContractorsReport),
            ResultType: typeof(ContractorsReport),
            AuthorisationType: typeof(UpdateContractorsReportAuthorisation),
            ValidationType: typeof(UpdateContractorsReportValidation),
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Every field is written as posted — read the record with get_contractors_report "
                + "first and carry forward what should not change. workOrderId values come from "
                + "the document's subcontractors[] (or list_work_orders); progress update ids from "
                + "updatesInPeriod[]. The report is never emailed by the portal: a person "
                + "downloads the Word or PDF from the page and sends it."),

        new AiAction(
            Name: "delete_contractors_report",
            Area: ContractorsReportArea,
            Description: "Deletes a Contractor's Report permanently. The progress updates, RFIs, "
                + "variations and work orders it read are untouched.",
            CommandType: typeof(DeleteContractorsReport),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteContractorsReportAuthorisation),
            ValidationType: null,
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which report, by number and week, before calling. There is no undo."),
    };
}
