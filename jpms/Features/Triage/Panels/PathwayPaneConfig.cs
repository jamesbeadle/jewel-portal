using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// What one pathway pane contains (the 2026-08-27 Control Centre restructure): which record types
/// its tagging sections link to, which record-less communication family gives it category
/// registers, and which system actions belong to its side. The four panes are one component
/// (PathwayPane) fed by these four constants, so the pathway split lives in data, not in four
/// hand-written panes.
/// </summary>
public sealed record PathwayPaneConfig(
    string Pathway,
    string Hint,
    IReadOnlyList<RecordType> LinkTypes,
    CommunicationFamily? Family,
    IReadOnlyList<(string Title, IReadOnlyList<SystemActionKind> Kinds)> ActionGroups)
{
    /// <summary>Every action kind this pane offers — for badging staged actions per pathway.</summary>
    public IReadOnlyList<SystemActionKind> AllActionKinds { get; } =
        ActionGroups.SelectMany(group => group.Kinds).ToList();

    // The pathway-neutral actions every pane closes with: arranging something dated and keeping
    // a contact on file belong to whichever side the email came from.
    private const string GeneralGroup = "General";
    private static readonly SystemActionKind[] GeneralKinds =
    {
        SystemActionKind.RaiseCalendarEvent,
        SystemActionKind.AddDirectoryContact,
    };

    public static PathwayPaneConfig Client { get; } = new(
        "Client",
        "The client, or their architect and team",
        new[]
        {
            RecordType.Request, RecordType.Variation, RecordType.TenderEnquiry,
            RecordType.BuildingControlInspection, RecordType.BuildingControlCase,
            RecordType.Lad, RecordType.ValuationReportSnapshot
        },
        Family: null,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[]
            {
                SystemActionKind.RaiseRfi,
                SystemActionKind.LogTenderEnquiry,
                SystemActionKind.RaiseVariationOrder,
                SystemActionKind.RaiseBuildingControlInspection,
            }),
            (SystemActionGuide.MoveGroup, new[]
            {
                SystemActionKind.PromoteRequestToRfi,
                SystemActionKind.ReopenRfi,
                SystemActionKind.CloseRfi,
                SystemActionKind.ApproveVariationOrder,
                SystemActionKind.RejectVariationOrder,
            }),
            (SystemActionGuide.PeopleGroup, new[] { SystemActionKind.ForwardToQs }),
            (GeneralGroup, GeneralKinds),
        });

    public static PathwayPaneConfig Subcontractor { get; } = new(
        "Subcontractor",
        "A subcontractor — the trades Jewel places work with",
        new[] { RecordType.BidPackageInvite, RecordType.WorkOrder, RecordType.Defect },
        CommunicationFamily.Subcontractor,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[]
            {
                SystemActionKind.RaiseWorkOrder,
                SystemActionKind.CreateBidPackageInvite,
                SystemActionKind.RaiseDefect,
            }),
            (SystemActionGuide.MoveGroup, new[] { SystemActionKind.FileBidPackageTender }),
            (GeneralGroup, GeneralKinds),
        });

    public static PathwayPaneConfig Supplier { get; } = new(
        "Supplier",
        "A materials or goods supplier, as distinct from a subcontractor",
        Array.Empty<RecordType>(),
        CommunicationFamily.Supplier,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (GeneralGroup, GeneralKinds),
        });

    // Bid packages and work orders came OUT of the Internal link types in this restructure
    // (Nigel, 2026-08-27): they are subcontractor records and live on the Subcontractor pane.
    public static PathwayPaneConfig Internal { get; } = new(
        "Internal",
        "Jewel staff — company admin",
        new[] { RecordType.Todo, RecordType.CalendarEvent },
        CommunicationFamily.Internal,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.PeopleGroup, new[]
            {
                SystemActionKind.CreateTodos,
                SystemActionKind.CompleteTodo,
            }),
            (GeneralGroup, GeneralKinds),
        });

    public static IReadOnlyList<PathwayPaneConfig> All { get; } =
        new[] { Client, Subcontractor, Supplier, Internal };

    /// <summary>Singular UI label for a link type's section — Relevant Event (not Scheduling),
    /// LADs claim, Variation Order (one record): the same terminology map the old System Tags
    /// pane carried.</summary>
    public static string TypeLabel(RecordType type) => type switch
    {
        RecordType.Request => "Request / RFI",
        RecordType.Variation => "Variation Order",
        RecordType.TenderEnquiry => "Tender Enquiry",
        RecordType.Lad => "LADs claim",
        RecordType.ValuationReportSnapshot => "Valuation report snapshot",
        RecordType.Scheduling => "Relevant Event",
        RecordType.BidPackageInvite => "Bid Package Invite",
        RecordType.WorkOrder => "Work Order",
        RecordType.Defect => "Defect",
        RecordType.Todo => "To-do item",
        RecordType.CalendarEvent => "Calendar event",
        RecordType.BuildingControlInspection => "Building Control Inspection",
        RecordType.BuildingControlCase => "Building Control Case",
        RecordType.SubcontractorComms => "Subcontractor communication",
        RecordType.SupplierComms => "Supplier communication",
        RecordType.InternalComms => "Internal communication",
        _ => type.ToString()
    };

    /// <summary>Plural section title ("Requests / RFIs") — light-touch, keyed where a bare "s"
    /// doesn't read.</summary>
    public static string TypeLabelPlural(RecordType type) => type switch
    {
        RecordType.Request => "Requests / RFIs",
        RecordType.Lad => "LADs claims",
        _ => TypeLabel(type) + "s"
    };
}
