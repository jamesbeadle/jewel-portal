namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// How the System Actions dropdown reads (Nigel, 2026-08-22): fourteen flat rows left him unsure
/// whether an incoming tender was a Raise RFI, a File Bid Package Tender or a to-do. The rows are
/// grouped by what they do to the system, and the chosen action says in one line when it's the
/// right one.
/// </summary>
public static class SystemActionGuide
{
    public const string RaiseGroup = "Raise something new";
    public const string MoveGroup = "Move an existing record on";
    public const string PeopleGroup = "People and to-dos";

    /// <summary>Dropdown groups, in display order, each with its actions in Nigel's order.</summary>
    public static IReadOnlyList<(string Title, IReadOnlyList<SystemActionKind> Kinds)> Groups { get; } =
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (RaiseGroup, new[]
            {
                SystemActionKind.RaiseRfi,
                SystemActionKind.LogTenderEnquiry,
                SystemActionKind.RaiseVariationOrder,
                SystemActionKind.RaiseWorkOrder,
                SystemActionKind.CreateBidPackageInvite,
                SystemActionKind.RaiseDefect,
                SystemActionKind.RaiseCalendarEvent,
            }),
            (MoveGroup, new[]
            {
                SystemActionKind.PromoteRequestToRfi,
                SystemActionKind.ReopenRfi,
                SystemActionKind.CloseRfi,
                SystemActionKind.ApproveVariationOrder,
                SystemActionKind.RejectVariationOrder,
                SystemActionKind.FileBidPackageTender,
            }),
            (PeopleGroup, new[]
            {
                SystemActionKind.CreateTodos,
                SystemActionKind.CompleteTodo,
                SystemActionKind.AddDirectoryContact,
                SystemActionKind.ForwardToQs,
            }),
        };

    public static string WhenToUse(SystemActionKind kind) => kind switch
    {
        SystemActionKind.RaiseRfi => "A question for the client side that needs a formal, numbered answer — an architect's detail, a spec gap, a sequencing clash.",
        SystemActionKind.LogTenderEnquiry => "An architect or client inviting Jewel to tender for a job — a PQQ, an expression-of-interest, a tender pack. Creates the Lead project if the job is new, keeps the PQQ and drawings, and tracks the bid from here to won or lost.",
        SystemActionKind.RaiseVariationOrder => "The client side has asked for, or caused, extra or changed work that needs pricing and approval.",
        SystemActionKind.RaiseWorkOrder => "You're placing work with a subcontractor — the email is the agreed scope or price; Apply emails them the purchase order.",
        SystemActionKind.CreateBidPackageInvite => "A package of work you're about to put out to tender — sets up the package so subcontractors can be invited.",
        SystemActionKind.RaiseDefect => "Something a subcontractor has to put right — logs it on the project's Defects tab and chases it with them.",
        SystemActionKind.RaiseCalendarEvent => "This email is arranging something dated — a site visit, a delivery, a meeting, subcontractor attendance. Puts it on the project's Calendar tab so everyone sees it coming.",
        SystemActionKind.PromoteRequestToRfi => "An older General request that has turned out to need a formal RFI number.",
        SystemActionKind.ReopenRfi => "An answered RFI that this email reopens — the answer was wrong, or more has come up.",
        SystemActionKind.CloseRfi => "This email answers an open RFI — file the answer and close it.",
        SystemActionKind.ApproveVariationOrder => "The client side has approved a variation in this email.",
        SystemActionKind.RejectVariationOrder => "The client side has turned a variation down in this email.",
        SystemActionKind.FileBidPackageTender => "A subcontractor has returned pricing for a package you invited them to. (An architect inviting Jewel to tender is Log Tender Enquiry, not this.)",
        SystemActionKind.CreateTodos => "Something for someone at Jewel to do — the catch-all when no record fits.",
        SystemActionKind.CompleteTodo => "This email shows a to-do is done — tick it off.",
        SystemActionKind.AddDirectoryContact => "A new supplier, subcontractor or contact to keep on file from this email.",
        SystemActionKind.ForwardToQs => "Pass this to the QS — a tender enquiry, pricing or a quote they need to pick up. Lines up a forward with the QS pre-filled; Apply sends it.",
        _ => ""
    };
}
