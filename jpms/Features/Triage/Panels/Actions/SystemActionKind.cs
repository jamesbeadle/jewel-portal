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
    RaiseDefect,
    CreateTodos,
    CompleteTodo,
    AddDirectoryContact
}

public static class SystemActionKinds
{
    /// <summary>Dropdown order — Nigel's list, minus the retired Raise Request (2026-08-14);
    /// Raise RFI leads as the default create.</summary>
    public static readonly SystemActionKind[] All =
    {
        SystemActionKind.RaiseRfi,
        SystemActionKind.PromoteRequestToRfi,
        SystemActionKind.ReopenRfi,
        SystemActionKind.CloseRfi,
        SystemActionKind.RaiseVariationOrder,
        SystemActionKind.ApproveVariationOrder,
        SystemActionKind.RejectVariationOrder,
        SystemActionKind.RaiseWorkOrder,
        SystemActionKind.CreateBidPackageInvite,
        SystemActionKind.RaiseDefect,
        SystemActionKind.CreateTodos,
        SystemActionKind.CompleteTodo,
        SystemActionKind.AddDirectoryContact
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
        SystemActionKind.RaiseDefect => "Raise Defect",
        SystemActionKind.CreateTodos => "Create To-do Items",
        SystemActionKind.CompleteTodo => "Mark To-do Done",
        SystemActionKind.AddDirectoryContact => "Add Directory Contact",
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
/// stage-dependent) — so SystemActionsPane can stage the record's tag the moment the action
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
}
