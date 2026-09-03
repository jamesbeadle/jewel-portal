namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// Every action the System Actions pane offers — one entry per dropdown row. The record creates
/// (request/RFI, work order, bid package, defect, to-dos) draft the page's email-linked staging,
/// so Apply tags the email to what it raises; the rest reuse the component (or store call) their
/// home page uses, staged as closures. Either way the behaviour is defined in one place.
/// </summary>
public enum SystemActionKind
{
    // RaiseRequest retired 2026-08-14 — General requests are sunset: raise a to-do for things
    // that aren't an RFI. Existing requests can still be promoted/worked; nothing raises new ones.
    PromoteRequestToRfi,
    RaiseRfi,
    ReopenRfi,
    CloseRfi,
    RaiseVariationOrder,
    ApproveVariationOrder,
    RejectVariationOrder,
    RaiseWorkOrder,
    CreateBidPackageInvite,
    FileBidPackageTender,
    RaiseDefect,
    CreateTodos,
    CompleteTodo,
    AddDirectoryContact,
    // An architect inviting Jewel to tender (2026-08-25): logs the enquiry — creating its
    // Lead-stage project when the job is new — with the PQQ and drawings copied off the email.
    LogTenderEnquiry,
    // An email arranging something dated (2026-08-27) — a site visit, a delivery, a meeting:
    // raises it on the project's Calendar tab with the email tagged to it.
    RaiseCalendarEvent,
    // The building control inspector's email (2026-08-27) — a booking confirmation, a visit
    // arrangement: raises the stage on the project's Building Control tab with the email tagged
    // to it (JPMS/BCI-####).
    RaiseBuildingControlInspection,
    // A supplier's email about goods for the job (2026-08-28) — a delivery note, an order
    // confirmation: adds the item (product + location details) to the project's Inventory tab
    // with the email tagged to it (JPMS/INV-####).
    AddInventoryItem,
    // An email worth keeping as evidence of how someone at Jewel is performing (2026-09-03):
    // files it as a KPI under a portal user. ADMINISTRATORS ONLY — the row is offered to no
    // other role. The mark lives in the KPI register (Admin → KPI emails) alone; the email is
    // tagged only JPMS/Admin (+ Internal pathway) so it leaves the queue — nobody else can tell
    // it is a KPI. Spread follows the Control Centre's "Entire thread" answer at Apply.
    MarkAsKpi
}

public static class SystemActionKinds
{
    /// <summary>Dropdown order — Nigel's list, minus the retired Raise Request (2026-08-14);
    /// Raise RFI leads as the default create.</summary>
    public static readonly SystemActionKind[] All =
    {
        SystemActionKind.RaiseRfi,
        SystemActionKind.LogTenderEnquiry,
        SystemActionKind.PromoteRequestToRfi,
        SystemActionKind.ReopenRfi,
        SystemActionKind.CloseRfi,
        SystemActionKind.RaiseVariationOrder,
        SystemActionKind.ApproveVariationOrder,
        SystemActionKind.RejectVariationOrder,
        SystemActionKind.RaiseWorkOrder,
        SystemActionKind.CreateBidPackageInvite,
        SystemActionKind.FileBidPackageTender,
        SystemActionKind.RaiseDefect,
        SystemActionKind.AddInventoryItem,
        SystemActionKind.RaiseCalendarEvent,
        SystemActionKind.RaiseBuildingControlInspection,
        SystemActionKind.CreateTodos,
        SystemActionKind.CompleteTodo,
        SystemActionKind.AddDirectoryContact,
        SystemActionKind.MarkAsKpi
    };

    public static string Label(SystemActionKind kind) => kind switch
    {
        SystemActionKind.PromoteRequestToRfi => "Promote Request to RFI",
        SystemActionKind.RaiseRfi => "Raise RFI",
        SystemActionKind.ReopenRfi => "Reopen RFI",
        SystemActionKind.CloseRfi => "Close RFI",
        SystemActionKind.RaiseVariationOrder => "Raise Variation Order",
        SystemActionKind.ApproveVariationOrder => "Approve Variation Order",
        SystemActionKind.RejectVariationOrder => "Reject Variation Order",
        SystemActionKind.RaiseWorkOrder => "Raise Work Order",
        SystemActionKind.CreateBidPackageInvite => "Create Bid Package Invite",
        SystemActionKind.FileBidPackageTender => "File Bid Package Tender",
        SystemActionKind.RaiseDefect => "Raise Defect",
        SystemActionKind.AddInventoryItem => "Add Inventory Item",
        SystemActionKind.RaiseCalendarEvent => "Raise Calendar Event",
        SystemActionKind.RaiseBuildingControlInspection => "Raise Building Control Inspection",
        SystemActionKind.CreateTodos => "Create To-do Items",
        SystemActionKind.CompleteTodo => "Mark To-do Done",
        SystemActionKind.AddDirectoryContact => "Add Directory Contact",
        SystemActionKind.LogTenderEnquiry => "Log Tender Enquiry",
        SystemActionKind.MarkAsKpi => "Mark as KPI",
        _ => kind.ToString()
    };
}

/// <summary>
/// One action lined up to fire when the email's triage is applied: what it is (for the row and
/// the badge), what it will do (the summary sentence), and the deferred work itself — a closure
/// whose payload was snapshotted at stage time, so editing the form afterwards changes nothing.
///
/// An action on an EXISTING record (an RFI transition, a variation decision) also carries that
/// record as <see cref="Target"/> — the server's own linkable projection, fetched by the stage
/// component, never hand-built (tag stems are provider business: project-qualified and
/// stage-dependent) — so PathwayActionsSection can stage the record's tag the moment the action
/// stages: actioning a record from an email files the email against it, same as ticking it in
/// System Tags by hand. <see cref="TargetAutoTagged"/> remembers whether it was this action that
/// added the pick (rather than the triager having picked it already), so removing the action
/// removes only its own tag.
/// </summary>
public sealed record StagedSystemAction(
    SystemActionKind Kind,
    string Summary,
    Func<Task> ExecuteAsync,
    Jewel.JPMS.Models.LinkableRecord? Target = null)
{
    public bool TargetAutoTagged { get; init; }

    /// <summary>Which pathway pane staged this action ("Client", "Supplier", …). Some kinds are
    /// offered on more than one pane (a directory contact can be logged from the Subcontractor
    /// or the Supplier side), so the badge and the pane's own staged list count by where the
    /// action was actually staged, never by kind alone. Null = staged outside a pane.</summary>
    public string? Pathway { get; init; }

    /// <summary>An identity for actions that must not be staged twice for the same target and
    /// have no LinkableRecord to carry it — "kpi:id:{personId}" for Mark as KPI (2026-09-03), so
    /// the Tagging tab's KPI section and the Actions form see one another's staging. Null for
    /// every other kind.</summary>
    public string? Key { get; init; }

    /// <summary>An action whose server command tags the email needs the Control Centre's "Entire
    /// thread" answer, which is only known at Apply (the pane stages before the pair is answered,
    /// and it can change afterwards). When set, Apply calls this with the resolved
    /// <see cref="Jewel.JPMS.Contracts.RecordLinks.LinkThreadScope"/> INSTEAD of
    /// <see cref="ExecuteAsync"/>. Null = the action tags nothing; ExecuteAsync runs.</summary>
    public Func<Jewel.JPMS.Contracts.RecordLinks.LinkThreadScope, Task>? ExecuteWithScopeAsync { get; init; }
}
