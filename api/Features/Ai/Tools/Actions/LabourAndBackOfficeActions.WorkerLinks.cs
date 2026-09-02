using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> WorkerLinkActions() => new AiAction[]
    {
        new AiAction(
            Name: "link_worker_to_company",
            Area: "Labour",
            Description: "Links a worker to a directory company — the settlement identity the "
                + "whole labour/Xero machinery keys on: covers, the settlement schedule and the "
                + "coding run all reconcile through it. Both names are matched server-side "
                + "(worker against the register, company against the non-prospect directory) and "
                + "an ambiguous name refuses with the candidates. Clears any sole-trader flag — "
                + "a company link always wins. Audited.",
            CommandType: typeof(LinkWorkerToCompanyByName),
            ResultType: typeof(Worker),
            AuthorisationType: typeof(LinkWorkerToCompanyByNameAuthorisation),
            ValidationType: typeof(LinkWorkerToCompanyByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName and companyName as the user says them. If the company exists only "
                + "in Xero, import it first (import_xero_supplier) — the import now auto-links "
                + "workers whose names match. For a worker who bills under their OWN name, "
                + "set_worker_sole_trader is the right fix, never an invented directory company."),

        new AiAction(
            Name: "set_worker_sole_trader",
            Area: "Labour",
            Description: "Flags (or with isSoleTrader: false unflags) a worker as a sole trader "
                + "who bills Dext/Xero under their own name — the worker then becomes their own "
                + "settlement counterparty: their bills can be marked as settlement, the "
                + "settlement schedule reconciles them, and the coding run stages draft bills "
                + "under their name. Refused while a company link exists (the link always wins — "
                + "clear it first). Audited.",
            CommandType: typeof(SetWorkerSoleTraderByName),
            ResultType: typeof(Worker),
            AuthorisationType: typeof(SetWorkerSoleTraderByNameAuthorisation),
            ValidationType: typeof(SetWorkerSoleTraderByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it. This is the designed answer for sole traders "
                + "(Midgley, Downey, Everitt, Jancauskas and the like) — never create a directory "
                + "company that does not exist just to unblock settlement."),

        new AiAction(
            Name: "reconcile_worker_directory_links",
            Area: "Labour",
            Description: "Sweeps every active worker with no settlement identity against the "
                + "company directory by name (the same matching the allocation page's labour "
                + "recognition uses) — the backfill for contacts imported before the import "
                + "auto-linked. apply: false reports what WOULD link, plus the ambiguous and "
                + "unmatched workers, without writing anything; apply: true writes the "
                + "unambiguous links (audited per worker) and still reports the remainder for a "
                + "human decision.",
            CommandType: typeof(ReconcileWorkerDirectoryLinks),
            ResultType: typeof(WorkerDirectoryLinkReport),
            AuthorisationType: typeof(ReconcileWorkerDirectoryLinksAuthorisation),
            ValidationType: typeof(ReconcileWorkerDirectoryLinksValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: new[] { "LinkedByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Run apply: false FIRST and show the user the would-link list — the confirm "
                + "turn then has real names in it, not a promise. Unmatched workers are usually "
                + "sole traders (set_worker_sole_trader) or companies still to import "
                + "(import_xero_supplier); ambiguous ones need link_worker_to_company with the "
                + "exact company name."),

        new AiAction(
            Name: "dismiss_labour_chase_day",
            Area: "Labour",
            Description: "Dismisses one worker's chase-list day with a mandatory reason — the "
                + "day was reviewed and needs no timesheet and no absence. The day leaves the "
                + "chase list AND the unconfirmed-cost accrual, so the confidence figures follow "
                + "the decision; the dismissal is written to the audit trail, and a timesheet or "
                + "absence recorded later supersedes it naturally.",
            CommandType: typeof(DismissLabourChaseDayByName),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DismissLabourChaseDayByNameAuthorisation),
            ValidationType: typeof(DismissLabourChaseDayByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: new[] { "DismissedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it; date is the single chase day. reason is "
                + "mandatory and shows on the audit trail — write the actual reason (\"not on "
                + "site that week\", \"engagement ended mid-month\"), not \"clearing the list\". "
                + "A worker wrongly chased EVERY day usually needs the real fix instead: "
                + "contracted days, a project assignment, or engagement dates on the worker. "
                + "restore_labour_chase_day is the undo."),

        new AiAction(
            Name: "restore_labour_chase_day",
            Area: "Labour",
            Description: "Removes a chase-day dismissal, putting the day back on the chase list "
                + "and back into the unconfirmed-cost accrual — the undo of "
                + "dismiss_labour_chase_day.",
            CommandType: typeof(RestoreLabourChaseDayByName),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RestoreLabourChaseDayByNameAuthorisation),
            ValidationType: typeof(RestoreLabourChaseDayByNameValidation),
            VisibleTo: LabourRoleSets.ManageWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it; date is the dismissed day."),
    };
}
