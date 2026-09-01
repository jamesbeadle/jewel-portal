using Jewel.JPMS.Api.Features.Inventory.Commands;
using Jewel.JPMS.Api.Features.WeeklyCashflow;
using Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;
using Jewel.JPMS.Contracts.Inventory;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The weekly cashflow plan and the inventory register as connector actions (2026-08-31). Both
/// suites shipped after the gateway's original sweep and were the parity audit's only true write
/// gaps (docs/ai/11 §2): full gate classes existed, but no declarations and no skip notes. The
/// weekly-cashflow doctrine that matters here: moving an entry changes WHEN it is paid, never how
/// much, and Xero remains the home of real payment agreements — a portal move is the fallback.
/// </summary>
public sealed class WeeklyCashflowAndInventoryActions : IAiActionSource
{
    public IEnumerable<AiAction> Build() => new[]
    {
        // ---- Cashflow: the accountant's 13-week payment plan --------------------------------

        new AiAction(
            Name: "create_weekly_cashflow_item",
            Area: "Cashflow",
            Description: "Adds a MANUAL item to the weekly cashflow plan — an outgoing or incoming "
                + "the ledgers don't carry (wages run, VAT, a known receipt), with a category, "
                + "amount, recurrence and first due date.",
            CommandType: typeof(CreateWeeklyCashflowItem),
            ResultType: typeof(WeeklyCashflowItem),
            AuthorisationType: typeof(CreateWeeklyCashflowItemAuthorisation),
            ValidationType: typeof(CreateWeeklyCashflowItemValidation),
            VisibleTo: WeeklyCashflowGates.WeeklyCashflowRoles,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Xero-fed bills and invoices arrive in the plan automatically — manual items are "
                + "only for flows Xero does not know about. Amounts in pounds."),

        new AiAction(
            Name: "update_weekly_cashflow_item",
            Area: "Cashflow",
            Description: "Rewrites a manual weekly-cashflow item's details — name, category, amount, "
                + "recurrence, dates, notes. Placements of its entries are kept.",
            CommandType: typeof(UpdateWeeklyCashflowItem),
            ResultType: typeof(WeeklyCashflowItem),
            AuthorisationType: typeof(UpdateWeeklyCashflowItemAuthorisation),
            ValidationType: typeof(UpdateWeeklyCashflowItemValidation),
            VisibleTo: WeeklyCashflowGates.WeeklyCashflowRoles,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Details replace the item's editable face whole — read the plan first and resend "
                + "every field."),

        new AiAction(
            Name: "archive_weekly_cashflow_item",
            Area: "Cashflow",
            Description: "Retires a manual item from the weekly cashflow plan — soft and stamped; "
                + "the row is kept but stops appearing in the grid.",
            CommandType: typeof(ArchiveWeeklyCashflowItem),
            ResultType: typeof(WeeklyCashflowItem),
            AuthorisationType: typeof(ArchiveWeeklyCashflowItemAuthorisation),
            ValidationType: null,
            VisibleTo: WeeklyCashflowGates.WeeklyCashflowRoles,
            EmailStamps: new[] { "ArchivedByEmail" },
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "place_weekly_cashflow_entry",
            Area: "Cashflow",
            Description: "Moves one weekly-cashflow entry to the week it will really be paid, or "
                + "resets it to its natural week — timing only; the amount never changes. "
                + "plannedWeekStart null clears the placement.",
            CommandType: typeof(PlaceWeeklyCashflowEntry),
            ResultType: typeof(WeeklyCashflowPlacementAnswer),
            AuthorisationType: typeof(PlaceWeeklyCashflowEntryAuthorisation),
            ValidationType: typeof(PlaceWeeklyCashflowEntryValidation),
            VisibleTo: WeeklyCashflowGates.WeeklyCashflowRoles,
            EmailStamps: new[] { "MovedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "placementKey comes from the plan's entries. A real payment agreement (retention, "
                + "agreed late payment) belongs in Xero as the bill's planned date — the grid follows; "
                + "a portal move is the fallback, not the norm."),

        new AiAction(
            Name: "set_weekly_cashflow_exclusion",
            Area: "Cashflow",
            Description: "Excludes one Xero-fed entry from the weekly cashflow plan (a bill that "
                + "will not actually be paid from this bank account), or restores it.",
            CommandType: typeof(SetWeeklyCashflowExclusion),
            ResultType: typeof(WeeklyCashflowExclusionAnswer),
            AuthorisationType: typeof(SetWeeklyCashflowExclusionAuthorisation),
            ValidationType: typeof(SetWeeklyCashflowExclusionValidation),
            VisibleTo: WeeklyCashflowGates.WeeklyCashflowRoles,
            EmailStamps: new[] { "ExcludedByEmail" },
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "save_weekly_cashflow_supplier_group",
            Area: "Cashflow",
            Description: "Creates or renames a weekly-cashflow supplier group — suppliers whose "
                + "bills the plan moves together as one row. Saving replaces the group's supplier "
                + "list whole.",
            CommandType: typeof(SaveWeeklyCashflowSupplierGroup),
            ResultType: typeof(WeeklyCashflowSupplierGroup),
            AuthorisationType: typeof(SaveWeeklyCashflowSupplierGroupAuthorisation),
            ValidationType: typeof(SaveWeeklyCashflowSupplierGroupValidation),
            VisibleTo: WeeklyCashflowGates.WeeklyCashflowRoles,
            EmailStamps: new[] { "SavedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "One supplier belongs to at most one group — the same name in two groups would "
                + "double-count its bills. supplierGroupId null creates; an existing id updates."),

        new AiAction(
            Name: "remove_weekly_cashflow_supplier_group",
            Area: "Cashflow",
            Description: "Removes a weekly-cashflow supplier group. The suppliers' bills stay in "
                + "the plan, ungrouped; their individual placements stand.",
            CommandType: typeof(DeleteWeeklyCashflowSupplierGroup),
            ResultType: typeof(WeeklyCashflowSupplierGroup),
            AuthorisationType: typeof(DeleteWeeklyCashflowSupplierGroupAuthorisation),
            ValidationType: null,
            VisibleTo: WeeklyCashflowGates.WeeklyCashflowRoles,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        // ---- Inventory: goods held for the job ----------------------------------------------

        new AiAction(
            Name: "add_inventory_item",
            Area: "Inventory",
            Description: "Adds an item to a project's inventory register — a product held for the "
                + "job and where it is kept. The item is given the next INV-#### reference, which "
                + "is also its mailbox tag stem.",
            CommandType: typeof(AddInventoryItem),
            ResultType: typeof(InventoryItem),
            AuthorisationType: typeof(AddInventoryItemAuthorisation),
            ValidationType: typeof(AddInventoryItemValidation),
            VisibleTo: RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "update_inventory_item",
            Area: "Inventory",
            Description: "Rewrites an inventory item's product and location details. The reference "
                + "never changes.",
            CommandType: typeof(UpdateInventoryItem),
            ResultType: typeof(InventoryItem),
            AuthorisationType: typeof(UpdateInventoryItemAuthorisation),
            ValidationType: typeof(UpdateInventoryItemValidation),
            VisibleTo: RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "All four detail fields are replaced together — read the register first and "
                + "resend what should not change."),

        new AiAction(
            Name: "create_inventory_item_from_message",
            Area: "Inventory",
            Description: "Adds an inventory item FROM a mailbox email (the Control Centre's "
                + "supplier pathway): creates the item exactly like add_inventory_item and links "
                + "the originating email to it by its INV-#### tag, so the item reads its mail "
                + "back like every other record.",
            CommandType: typeof(CreateInventoryItemFromMessage),
            ResultType: typeof(InventoryItem),
            AuthorisationType: typeof(CreateInventoryItemFromMessageAuthorisation),
            ValidationType: typeof(CreateInventoryItemFromMessageValidation),
            VisibleTo: RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is the mailbox message id; scope says how far the tag spreads across "
                + "the conversation (default ThreadBehindAnchor). Send allowCrossPathway true — the "
                + "pane choice is the decision and the guard is a no-op."),
    };

    // Skipped: AdjustTimesheet — gates exist (AdjustTimesheetSlice.cs), but the command is keyed
    //   by an opaque TimesheetId the connector cannot resolve; the recorded decision there is a
    //   by-name/date wrapper if a need appears. Not silently absent — recorded here.
    // Skipped: ApproveTimesheets / RejectTimesheet — superseded for the connector by the by-name
    //   actions approve_worker_week / reject_worker_day (2026-08-28); the id-keyed originals stay
    //   portal-only on purpose.
}
