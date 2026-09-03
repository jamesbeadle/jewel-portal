namespace Jewel.JPMS.Features.Triage;

/// <summary>The record-LINKING vocabulary every triage surface shares: which types an email
/// can be filed to, per pathway and in full, and how each type reads on screen — one list,
/// so the pickers, panes and composer can never disagree.</summary>
public static class RecordLinkVocabulary
{
    // Every record type an email can be LINKED to. Driven by the providers registered server-side;
    // adding a type here surfaces it once its provider exists. Under the pathway-first UI this full
    // list is only the fallback for a tagged thread that has no pathway yet — the pathway-filtered
    // subsets below are what the pickers normally offer.
    // Cost Centre was removed as a link target 2026-08-04 (it never earned its place as a filing
    // destination; existing CC-tagged mail keeps reading fine). VariationQuote is folded into
    // Variation — one record, one number, per the 2026-07-23 unification.
    public static readonly RecordType[] RecordTypeOptions =
    {
        RecordType.Request, RecordType.BidPackageInvite, RecordType.WorkOrder, RecordType.Scheduling, RecordType.Lad, RecordType.Variation, RecordType.Todo, RecordType.CalendarEvent, RecordType.BuildingControlInspection, RecordType.BuildingControlCase, RecordType.Inventory, RecordType.SiteInstruction
    };

    // What each pathway's "Link to existing" offers (the pathway filters the actions — the plan's
    // §2.2): client-side records only under Client, subcontract-side only under Subcontractor, and
    // Internal links to existing to-do items. CostCentre appears on BOTH sides of the wall because
    // cost-centre mail can be valuation-side or subcontract-side — the pathway choice decides, and
    // travels with the link command.
    public static readonly RecordType[] ClientLinkTypes =
    {
        RecordType.Request, RecordType.Variation, RecordType.Lad, RecordType.Scheduling,
        RecordType.BuildingControlInspection, RecordType.BuildingControlCase
    };
    // Subcontractor covers the whole subcontract lifecycle, not just the tender: an email can land
    // before a package exists (a chase, an H&S request), against the order that followed the award
    // (WorkOrder — the plan's §2.2 "link work order"), or onto a to-do that tracks something the
    // supplier owes us. Todo is offered here because to-do links are pathway-neutral, so tagging one
    // never re-files the thread — the same reason the Tagged picker offers it on every pathway.
    public static readonly RecordType[] SubcontractorLinkTypes =
    {
        RecordType.BidPackageInvite, RecordType.WorkOrder
    };
    // Site instructions (2026-09-03) are the Internal pathway's own project-scoped record —
    // Jewel instructing its site, written at triage or on the project's Site Instructions page.
    public static readonly RecordType[] InternalLinkTypes = { RecordType.Todo, RecordType.SiteInstruction, RecordType.CalendarEvent };
    // Inventory (2026-08-28) is the Supplier pathway's first linkable record type — goods for the
    // job, raised from a supplier email or on the project's Inventory tab.
    public static readonly RecordType[] SupplierLinkTypes = { RecordType.Inventory };

    // The Tagged tab's "link to another record" pool: since the hard client wall was removed
    // (2026-08-21) every type is offered whatever the thread's pathway. Each option's label shows
    // the pathway it files under, and a link that would file the thread under a second pathway
    // simply files it under both (the confirm step was retired 2026-08-28).

    // The pathway a record type files a thread under, as a TriagePathway — mirrors the server's
    // TriageCategories.BucketFor. Null = pathway-neutral (Todo) or per-email choice (CostCentre).
    // Drives the Tagged picker's cross-filing heads-up.
    public static TriagePathway? ImpliedPathway(RecordType type) => type switch
    {
        RecordType.Request or RecordType.Variation or RecordType.VariationQuote
            or RecordType.Scheduling or RecordType.Lad => TriagePathway.Client,
        RecordType.BidPackageInvite or RecordType.WorkOrder
            or RecordType.SubcontractorComms => TriagePathway.Subcontractor,
        RecordType.SupplierComms or RecordType.Inventory => TriagePathway.Supplier,
        RecordType.InternalComms or RecordType.SiteInstruction => TriagePathway.Internal,
        _ => null
    };

    // The pathway a record type files a thread under, for the Tagged picker's labels — mirrors the
    // server's TriageCategories.BucketFor so the triager sees where a link would put the thread.
    public static string RecordTypePathwayLabel(RecordType type) => type switch
    {
        RecordType.Request or RecordType.Variation or RecordType.VariationQuote
            or RecordType.Scheduling or RecordType.Lad
            or RecordType.BuildingControlCase or RecordType.BuildingControlInspection => "Client",
        RecordType.BidPackageInvite or RecordType.WorkOrder
            or RecordType.SubcontractorComms => "Subcontractor",
        RecordType.SupplierComms    => "Supplier",
        RecordType.Inventory        => "Supplier",
        RecordType.InternalComms    => "Internal",
        RecordType.SiteInstruction  => "Internal",
        RecordType.CostCentre       => "Client or Subcontractor",
        RecordType.Todo             => "Neutral",
        RecordType.CalendarEvent    => "Neutral",
        _ => ""
    };

    // The optional "link these to-dos to an open request" picker, offered on every pathway EXCEPT
    // Subcontractor. A request is a Client record: tagging one files the thread under Client, which is
    // right for an internal thread that turns out to be client business. (The hard client wall was
    // removed 2026-08-21 — the hiding is kept so the Subcontractor → To-dos action stays neutral by
    // default; subcontract mail that is really client business files via the Tagged picker's confirm.)

    public static string RecordTypeLabel(RecordType type) => type switch
    {
        RecordType.Request          => "Request",
        RecordType.BidPackageInvite => "Bid Package Invite",
        RecordType.WorkOrder        => "Work Order",
        RecordType.CostCentre       => "Cost Centre",
        // UI terminology is "Relevant Event" (what the programme bucket holds — decision
        // 2026-08-07); the RecordType/tag layer keeps its Scheduling identifiers.
        RecordType.Scheduling       => "Relevant Event",
        RecordType.Variation        => "Variation Order",
        RecordType.VariationQuote   => "Variation Order Quote",
        RecordType.Lad              => "LADs claim",
        RecordType.Todo             => "To-do item",
        RecordType.CalendarEvent    => "Calendar event",
        RecordType.BuildingControlInspection => "Building Control Inspection",
        RecordType.BuildingControlCase => "Building Control Case",
        RecordType.SubcontractorComms => "Subcontractor communication",
        RecordType.SupplierComms    => "Supplier communication",
        RecordType.InternalComms    => "Internal communication",
        RecordType.Inventory        => "Inventory item",
        RecordType.SiteInstruction  => "Site instruction",
        _                           => type.ToString()
    };

    // Lower-case plural for the generic "Loading …" / "No … on this project" copy. Scheduling lists
    // the project's bucket plus its claims documents (NOD/EOT/LADs), so its plural reads as that set.
    public static string RecordTypeLabelPlural(RecordType type) => type switch
    {
        RecordType.Scheduling => "relevant events and claims documents",
        RecordType.Variation      => "variation orders",
        RecordType.VariationQuote => "variation order quotes",
        RecordType.Lad        => "LADs claims",
        RecordType.Todo       => "to-do items",
        _                     => $"{RecordTypeLabel(type).ToLowerInvariant()}s"
    };

    public static string RecordTypeLabelSingular(RecordType type) => type switch
    {
        RecordType.Scheduling => "relevant event or claims document",
        RecordType.Variation      => "variation order",
        RecordType.VariationQuote => "variation order quote",
        RecordType.Lad        => "LADs claim",
        RecordType.Todo       => "to-do item",
        _                     => RecordTypeLabel(type).ToLowerInvariant()
    };

}
