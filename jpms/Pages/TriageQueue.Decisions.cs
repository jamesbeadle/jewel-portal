using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

using Jewel.JPMS.Features.Triage.Queue;
namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // The staged work + armed discard. The System Tags pane's tab mirrors the page's own
    // `pathway` field (the pathway decision); stagedCreate is the pane's drafted new record
    // (null = none) — StagedRecordKind decides whether Apply raises a request, a bid package, a work order or a defect.
    private bool discardArmed;
    private StagedRecordCreate? stagedCreate;
    // The "Relevant Event for Programme" decision — staged like everything else, applied by the
    // one Apply. Lives OUTSIDE System Tags because the programme bucket isn't a record anyone
    // picks or creates: every project has exactly one, so filing to it is a yes/no, not a search.
    // Nullable on purpose: null = not yet answered (the Yes/No pair renders blank), and Apply
    // refuses to run until the triager picks a side — a conscious decision, never a default.
    private bool? relevantEventStaged;
    // The "Entire thread" decision: Yes means every action in the apply spreads across the whole
    // current conversation (LinkThreadScope.EntireThread); No means each action tags only the
    // clicked email (MessageOnly). Nullable like the Relevant Event decision above — blank until
    // answered, required before Apply. Never persisted, cleared back to blank with the rest of
    // the staging on every selection/view change and after every apply.
    private bool? triageEntireThread;
    // The "Use existing tags" decision, offered only when the open email's thread ALREADY carries
    // record tags (the queue row's outline "Thread:" chips). Yes means Apply files this email
    // under those same records — the stems resolve back to records (ResolveRecordTags, the same
    // resolver behind the search chips) and each links exactly like a picked record — so a reply
    // to an already-linked thread is triaged in one answer, with nothing new to pick. No means
    // the triager picks this email's records themselves. Nullable like the two decisions above —
    // blank until answered, required before Apply whenever the row is on show.
    private bool? useThreadTags;

    // What the Subcontractor Communications browser tags against: the open QUEUE email (the
    // Tagged view manages its tags from the email pane instead), and the triage bar's project —
    // by name, because record-less communication tags carry no project to filter on.
    private string OpenQueueEmailSubject =>
        view == QueueView.Active && selected is not null ? selected.Subject : "";

    private string TriageProjectName =>
        AllProjects.FirstOrDefault(project => project.ProjectId == triageProjectId)?.Name ?? "";

    // Staging from a pathway pane IS the pathway decision (as the old System Tags tab switch
    // was) — parse the pane's label back onto the page's own pathway state so filing, to-dos and
    // a record-less reply all read one field.
    private void OnPathwayEngaged(string paneLabel)
    {
        if (Enum.TryParse<TriagePathway>(paneLabel, out var next)) SetPathway(next);
    }

    // Each pathway icon's badge = the staged work that pane owns: its record picks and category
    // ticks, its own staged actions, the drafted new record and the drafted to-dos. Every action
    // kind lives on exactly one pane (no shared "General" group — 2026-08-27 review), so the
    // kind→pane map is the pane configs themselves.
    private int PathwayBadge(PathwayPaneConfig config) =>
        pickedRecords.Count(record => config.LinkTypes.Contains(record.Type)
            || (config.Family is { } family && family.All.Any(familyRecord => familyRecord.RecordId == record.RecordId)))
        // Kinds can be offered on more than one pane (directory contact: Subcontractor,
        // Supplier, Internal), so staged actions count where they were STAGED, not by kind.
        + stagedSystemActions.Count(action => action.Pathway is { } stagedFrom
            ? stagedFrom == config.Pathway
            : config.AllActionKinds.Contains(action.Kind))
        + (config.Pathway == "Internal" ? CurrentTodoDrafts().Count : 0)
        + (StagedCreateReady && StagedCreatePathway(stagedCreate!.Kind) == config.Pathway ? 1 : 0);

    // Which pane's badge a drafted record counts on — mirrors which pane offers its create.
    private static string? StagedCreatePathway(StagedRecordKind kind) => kind switch
    {
        StagedRecordKind.Request or StagedRecordKind.TenderEnquiry
            or StagedRecordKind.BuildingControlInspection => "Client",
        StagedRecordKind.BidPackage or StagedRecordKind.WorkOrder or StagedRecordKind.Defect => "Subcontractor",
        StagedRecordKind.Inventory => "Supplier",
        StagedRecordKind.CalendarEvent => "Internal", // raised from the Internal pane, beside the Calendar
        _ => null
    };

    private bool StagedCreateReady => stagedCreate is { } sc && sc.IsReady;

    // A tender enquiry usually brings its own Lead project, so it needs no project in the bar.
    private bool StagedCreatesOwnProject =>
        stagedCreate is { Kind: StagedRecordKind.TenderEnquiry } sc && sc.TenderEnquiry.CreatesNewProject;

    // Joining an existing project is only ever the same job's second email — the bar's project
    // must itself still be a Lead.
    private bool TriageProjectIsLead =>
        !string.IsNullOrWhiteSpace(triageProjectId) && Projects.Find(triageProjectId)?.Stage == ProjectStage.Lead;

    private string? StagedTenderEnquiryProblem =>
        stagedCreate is { Kind: StagedRecordKind.TenderEnquiry } sc
            ? sc.TenderEnquiry.Problem(TriageProjectIsLead)
            : null;

    private string? StagedCalendarEventProblem =>
        stagedCreate is { Kind: StagedRecordKind.CalendarEvent } stagedEvent
            ? stagedEvent.CalendarEvent.Problem
            : null;

    private string? StagedBuildingControlInspectionProblem =>
        stagedCreate is { Kind: StagedRecordKind.BuildingControlInspection } stagedInspection
            ? stagedInspection.BuildingControlInspection.Problem
            : null;

    private string? TodoProjectNote =>
        string.IsNullOrWhiteSpace(triageProjectId)
            ? "No project set on the email — these will be company-wide items. Set the Project in the triage bar above to put them on a project's To-do tab."
            : $"Items land on the To-do tab of {ProjectLabelFor(triageProjectId)} — the email's project, set in the triage bar above.";
}
