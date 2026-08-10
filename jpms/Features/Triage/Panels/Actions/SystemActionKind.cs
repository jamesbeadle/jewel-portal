namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// Every action the System Actions pane can stage — one entry per dropdown row. Each reuses the
/// component (or store call) its home page uses, so the behaviour is defined in one place.
/// </summary>
public enum SystemActionKind
{
    RaiseRequest,
    PromoteRequestToRfi,
    RaiseRfi,
    ReopenRfi,
    CloseRfi,
    RaiseVariationOrder,
    ApproveVariationOrder,
    RejectVariationOrder,
    RaiseWorkOrder,
    CreateBidPackageInvite,
    AddDirectoryContact
}

public static class SystemActionKinds
{
    /// <summary>Dropdown order — Nigel's list, verbatim.</summary>
    public static readonly SystemActionKind[] All =
    {
        SystemActionKind.RaiseRequest,
        SystemActionKind.PromoteRequestToRfi,
        SystemActionKind.RaiseRfi,
        SystemActionKind.ReopenRfi,
        SystemActionKind.CloseRfi,
        SystemActionKind.RaiseVariationOrder,
        SystemActionKind.ApproveVariationOrder,
        SystemActionKind.RejectVariationOrder,
        SystemActionKind.RaiseWorkOrder,
        SystemActionKind.CreateBidPackageInvite,
        SystemActionKind.AddDirectoryContact
    };

    public static string Label(SystemActionKind kind) => kind switch
    {
        SystemActionKind.RaiseRequest => "Raise Request",
        SystemActionKind.PromoteRequestToRfi => "Promote Request to RFI",
        SystemActionKind.RaiseRfi => "Raise RFI",
        SystemActionKind.ReopenRfi => "Reopen RFI",
        SystemActionKind.CloseRfi => "Close RFI",
        SystemActionKind.RaiseVariationOrder => "Raise Variation Order",
        SystemActionKind.ApproveVariationOrder => "Approve Variation Order",
        SystemActionKind.RejectVariationOrder => "Reject Variation Order",
        SystemActionKind.RaiseWorkOrder => "Raise Work Order",
        SystemActionKind.CreateBidPackageInvite => "Create Bid Package Invite",
        SystemActionKind.AddDirectoryContact => "Add Directory Contact",
        _ => kind.ToString()
    };
}

/// <summary>
/// One action lined up to fire when the email's triage is applied: what it is (for the row and
/// the badge), what it will do (the summary sentence), and the deferred work itself — a closure
/// whose payload was snapshotted at stage time, so editing the form afterwards changes nothing.
/// </summary>
public sealed record StagedSystemAction(SystemActionKind Kind, string Summary, Func<Task> ExecuteAsync);
