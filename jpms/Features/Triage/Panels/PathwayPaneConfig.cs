
namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// What one pathway pane contains (the 2026-08-27 Control Centre restructure): which record types
/// its tagging sections link to, which record-less communication family gives it category
/// registers, and which system actions belong to its side. The five panes are one component
/// (PathwayPane) fed by these five constants, so the pathway split lives in data, not in five
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

    public static PathwayPaneConfig Client { get; } = new(
        "Client",
        "The client, or their architect and team",
        // ValuationClaim is the one "Valuation reports" section: one row per period — the claim
        // IS the valuation and its statement (2026-09-18; before that a snapshot row stood in for
        // a period with a statement out, and two drawers for one period was the choice triagers
        // kept getting wrong). Mail still tagged to a retired snapshot counts under this section.
        new[]
        {
            RecordType.Request, RecordType.Variation,
            RecordType.BuildingControlInspection, RecordType.BuildingControlCase,
            RecordType.Lad, RecordType.ValuationClaim
        },
        Family: null,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[]
            {
                SystemActionKind.RaiseRfi,
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
            // Handing a client-side email to someone at Jewel — a tender enquiry the QS or Sales &
            // Marketing should pick up, say — is a to-do assigned to them (2026-09-03: "Forward to
            // QS" retired, and the same day "Log Tender Enquiry" with it: the assignee sees the
            // email on the to-do). No register of forwards or enquiries is kept.
            (SystemActionGuide.PeopleGroup, new[] { SystemActionKind.CreateTodos }),
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
            // Logging the trade you're emailing is a subcontractor-side act — the same kind is
            // offered on the Supplier and Internal panes; StagedSystemAction.Pathway keeps the
            // badges honest about which pane staged it.
            (SystemActionGuide.PeopleGroup, new[] { SystemActionKind.AddDirectoryContact }),
        });

    public static PathwayPaneConfig Supplier { get; } = new(
        "Supplier",
        "A materials or goods supplier, as distinct from a subcontractor",
        // Inventory (2026-08-28) is the pane's first linkable record type: goods for the job —
        // what the product is, where it's kept.
        // Defects joined it 2026-09-07 (James): faulty goods and short deliveries are put right by
        // the MERCHANT, not a trade, so a defect must be raisable and taggable from the supplier's
        // own email. The record is the same DEF-#### on the same Defects register — a defect names
        // the directory company it is raised with, and that company can be either category.
        // Work orders joined it 2026-09-15 (Nigel): the purchase order Jewel places with a
        // merchant is the SAME record as the one it places with a trade — one Work Orders tab,
        // one WO sequence, one PO PDF — so rather than build a separate "purchase order" feature
        // the one record is offered on both panes. Unlike a defect, its thread files by the
        // COMPANY the order is placed with (a Supplier-category company → Supplier;
        // CompanyPathways server-side), so a merchant's PO correspondence reads on this side.
        new[] { RecordType.WorkOrder, RecordType.Inventory, RecordType.Defect },
        CommunicationFamily.Supplier,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[]
            {
                SystemActionKind.RaiseWorkOrder,
                SystemActionKind.AddInventoryItem,
                SystemActionKind.RaiseDefect,
                SystemActionKind.RaiseCalendarEvent,
            }),
            (SystemActionGuide.PeopleGroup, new[] { SystemActionKind.AddDirectoryContact }),
        });

    // The prospect side (2026-09-15, Nigel): an estimate enquiry forwarded to the projects mailbox
    // is tagged to the sales lead it is about — an existing lead, or a new one raised from the
    // email — so the lead reads its mail live like every record, and the estimate is opened on
    // the lead's page. A lead belongs to NO project (RecordLinkVocabulary.IsCompanyWide), so the
    // pane never asks for the email's project. No record-less registers on this side.
    public static PathwayPaneConfig Sales { get; } = new(
        "Sales",
        "A prospect — someone who might build with Jewel",
        new[] { RecordType.Lead },
        Family: null,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[] { SystemActionKind.RaiseLead }),
        });

    // Bid packages and work orders came OUT of the Internal link types in this restructure
    // (Nigel, 2026-08-27): they are subcontractor records and live on the Subcontractor pane.
    // Site instructions (2026-09-03, James) became a real record here — like a to-do, raised
    // with written detail in Actions and tagged to from the Tagging list — replacing the
    // record-less "Tag as Site instruction" category tick, which said nothing about what the
    // instruction was.
    public static PathwayPaneConfig Internal { get; } = new(
        "Internal",
        "Jewel staff — company admin",
        new[] { RecordType.Todo, RecordType.SiteInstruction, RecordType.CalendarEvent },
        CommunicationFamily.Internal,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[]
            {
                SystemActionKind.RaiseSiteInstruction,
                SystemActionKind.RaiseCalendarEvent,
            }),
            (SystemActionGuide.PeopleGroup, new[]
            {
                SystemActionKind.CreateTodos,
                SystemActionKind.CompleteTodo,
                SystemActionKind.AddDirectoryContact,
                // Administrators only (SystemActionGuide.AdministratorOnly) — the section
                // drops it from the dropdown for every other role.
                SystemActionKind.MarkAsKpi,
            }),
        });

    public static IReadOnlyList<PathwayPaneConfig> All { get; } =
        new[] { Client, Subcontractor, Supplier, Sales, Internal };

    /// <summary>Singular UI label for a link type's section — Relevant Event (not Scheduling),
    /// LADs claim, Variation Order (one record): the same terminology map the old System Tags
    /// pane carried.</summary>
    public static string TypeLabel(RecordType type) => type switch
    {
        RecordType.Request => "Request / RFI",
        RecordType.Variation => "Variation Order",
        RecordType.Lad => "LADs claim",
        RecordType.ValuationClaim => "Valuation report",           // the merged section — statement or live period
        RecordType.ValuationReportSnapshot => "Valuation report", // retired type — resolves to the claim
        RecordType.Scheduling => "Relevant Event",
        RecordType.BidPackageInvite => "Bid Package Invite",
        RecordType.WorkOrder => "Work Order",
        RecordType.Defect => "Defect",
        RecordType.Inventory => "Inventory item",
        RecordType.SiteInstruction => "Site instruction",
        RecordType.Todo => "To-do item",
        RecordType.CalendarEvent => "Calendar event",
        RecordType.Lead => "Lead",
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
