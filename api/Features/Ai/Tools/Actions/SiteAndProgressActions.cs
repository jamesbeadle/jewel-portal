using Jewel.JPMS.Api.Features.Closeout.Commands;
using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>To-do, site, progress/programme, drawing and closeout commands as connector actions.
/// Mirrors Features/Todos, Features/Site, Features/Progress, Features/Drawings and
/// Features/Closeout — each entry's VisibleTo copies its Authorisation class's role set, and the
/// stamps copy exactly what the endpoint stamps server-side. The to-do commands AiWriteTools
/// already exposes as first-class tools (add_todo → AddTodoItem/AddGeneralTodoItem, complete_todo
/// → UpdateTodoItem, log_todo_progress → LogTodoProgress) are deliberately absent here.</summary>
internal sealed class SiteAndProgressActions : IAiActionSource
{
    // Replicates the private Director+PM sets RolesThatMayEditProgramme (AddProgrammeTask,
    // AddProgrammeTaskLink, RemoveProgrammeTask, RemoveProgrammeTaskLink), RolesThatMayBaseline
    // (TakeProgrammeBaseline, RemoveProgrammeBaseline) and RolesThatMayApproveReports
    // (ApproveSiteReport) in the Site authorisations.
    private static readonly RoleSet ProgrammePlanners =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    // Replicates the private Director+PM+SiteManager sets RolesThatMayAssembleReports
    // (AssembleSiteReportAuthorisation), RolesThatMayEditProgramme
    // (UpdateProgrammeTaskAuthorisation), RolesThatMayRaiseDefectsFromMail
    // (CreateDefectFromMessageAuthorisation) and RolesThatMayUpdateDefects
    // (UpdateDefectAuthorisation).
    private static readonly RoleSet SiteTeamManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);

    // Replicates the private RolesThatMayManageDrawings set in ApproveDrawingRevision,
    // DeleteDrawing and DeleteDrawingRevision authorisations — Administrator, Managing Director,
    // Project Manager.
    private static readonly RoleSet DrawingManagers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    // Replicates the private RolesThatMayRegisterDrawings (RegisterDrawingAuthorisation) and
    // RolesThatMayManageFolders (the DrawingFolderCommandAuthorisation classes) sets.
    private static readonly RoleSet DrawingRegisterCurators =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replicates the private RolesThatMayEditDrawings set in SetDrawingRevisionLabel and
    // UpdateDrawingMetadata authorisations — Director, Project Manager.
    private static readonly RoleSet DrawingEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    // Replicates the private Director+FinanceDirector sets RolesThatMayAgreeSettlement,
    // RolesThatMayAgreeVat and RolesThatMayReleaseRetention in the Closeout authorisations.
    private static readonly RoleSet CloseoutDirectors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    // Replicates the private RolesThatMayRaiseDefects set in RaiseDefectAuthorisation — includes
    // the external Client and Architect, who may raise defects through their own pages.
    private static readonly RoleSet DefectRaisers = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager,
        JpmsRoles.Client, JpmsRoles.Architect);

    public IEnumerable<AiAction> Build() => new[]
    {
        // ── To-dos ────────────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "create_todo_items_from_message",
            Area: "To-dos",
            Description: "Creates one or more to-do items from a mailbox message (triage pathway) "
                + "and tags the email \"JPMS/TODO-####\" for every item — the email is the items' "
                + "only record, no copy is stored. A blank projectId makes them company-wide "
                + "general items.",
            CommandType: typeof(CreateTodoItemsFromMessage),
            ResultType: typeof(IReadOnlyList<TodoItem>),
            AuthorisationType: typeof(CreateTodoItemsFromMessageAuthorisation),
            ValidationType: typeof(CreateTodoItemsFromMessageValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue, not a request id. "
                + "When linkRequestId is set the email is also tagged to that request first — it "
                + "must exist, be on the same project and not be Closed."),

        new AiAction(
            Name: "delete_todo_item",
            Area: "To-dos",
            Description: "Deletes a to-do item permanently, together with its activity timeline "
                + "and any to-do-to-to-do links naming it. There is no undo.",
            CommandType: typeof(DeleteTodoItem),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteTodoItemAuthorisation),
            ValidationType: typeof(DeleteTodoItemValidation),
            VisibleTo: TodoRoles.AllowedToManageTodos,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which item, by title, before calling. todoItemId comes "
                + "from list_todos or find_by_reference."),

        new AiAction(
            Name: "link_todo_items",
            Area: "To-dos",
            Description: "Links two to-do items so each lists the other as related work. Changes "
                + "how the work reads on the To-dos pages; nothing else on either item moves.",
            CommandType: typeof(LinkTodoItems),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(LinkTodoItemsAuthorisation),
            ValidationType: typeof(LinkTodoItemsValidation),
            VisibleTo: TodoRoles.AllowedToManageTodos,
            EmailStamps: new[] { "LinkedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Both ids come from list_todos or find_by_reference."),

        new AiAction(
            Name: "move_todo_item",
            Area: "To-dos",
            Description: "Re-files a to-do item under a different project (or company-wide with a "
                + "blank projectId) and touches nothing else — assignee, due date, linked emails "
                + "and open/done state all stay as they were.",
            CommandType: typeof(MoveTodoItem),
            ResultType: typeof(TodoItem),
            AuthorisationType: typeof(MoveTodoItemAuthorisation),
            ValidationType: typeof(MoveTodoItemValidation),
            VisibleTo: TodoRoles.AllowedToManageTodos,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Moving to COMPANY-WIDE (blank projectId) is narrower — managing director and "
                + "administrators only. Further per-record checks apply at execution."),

        new AiAction(
            Name: "unlink_todo_items",
            Area: "To-dos",
            Description: "Removes the link between two to-do items. The items themselves are "
                + "untouched.",
            CommandType: typeof(UnlinkTodoItems),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(UnlinkTodoItemsAuthorisation),
            ValidationType: typeof(UnlinkTodoItemsValidation),
            VisibleTo: TodoRoles.AllowedToManageTodos,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        // ── Site (site reports) ───────────────────────────────────────────────────────────────

        new AiAction(
            Name: "assemble_site_report",
            Area: "Site",
            Description: "Creates a new site report for a project (period end, narrative, "
                + "attendance days, open snags, progress percent). The report starts un-issued — "
                + "approve_site_report issues it.",
            CommandType: typeof(AssembleSiteReport),
            ResultType: typeof(SiteReport),
            AuthorisationType: typeof(AssembleSiteReportAuthorisation),
            ValidationType: typeof(AssembleSiteReportValidation),
            VisibleTo: SiteTeamManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Dates are ISO 8601."),

        new AiAction(
            Name: "approve_site_report",
            Area: "Site",
            Description: "Approves (issues) a site report — marks it issued for the project "
                + "record. This is a sign-off.",
            CommandType: typeof(ApproveSiteReport),
            ResultType: typeof(SiteReport),
            AuthorisationType: typeof(ApproveSiteReportAuthorisation),
            ValidationType: typeof(ApproveSiteReportValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which report, by project and period, before calling."),

        // ── Progress & programme (programme of works) ─────────────────────────────────────────

        new AiAction(
            Name: "add_programme_task",
            Area: "Progress & programme",
            Description: "Adds a task to a project's programme of works (title, planned start and "
                + "end, optional BoQ line link). Visible on the programme immediately.",
            CommandType: typeof(AddProgrammeTask),
            ResultType: typeof(ProgrammeTask),
            AuthorisationType: typeof(AddProgrammeTaskAuthorisation),
            ValidationType: typeof(AddProgrammeTaskValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Dates are ISO 8601."),

        new AiAction(
            Name: "update_programme_task",
            Area: "Progress & programme",
            Description: "Updates a programme task's title, planned dates, progress percent or "
                + "BoQ line link. Movement against the latest baseline is recalculated from the "
                + "new dates.",
            CommandType: typeof(UpdateProgrammeTask),
            ResultType: typeof(ProgrammeTask),
            AuthorisationType: typeof(UpdateProgrammeTaskAuthorisation),
            ValidationType: typeof(UpdateProgrammeTaskValidation),
            VisibleTo: SiteTeamManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Send the full task — every field is written as posted, so read the current "
                + "task first and carry forward what should not change."),

        new AiAction(
            Name: "remove_programme_task",
            Area: "Progress & programme",
            Description: "Removes a live programme task permanently, together with any dependency "
                + "links that touch it. Baseline snapshots of the task are deliberately kept as "
                + "the contemporaneous record.",
            CommandType: typeof(RemoveProgrammeTask),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveProgrammeTaskAuthorisation),
            ValidationType: typeof(RemoveProgrammeTaskValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which task, by title, before calling. There is no undo."),

        new AiAction(
            Name: "add_programme_task_link",
            Area: "Progress & programme",
            Description: "Adds a dependency link between two programme tasks (predecessor to "
                + "successor, with a lag in days).",
            CommandType: typeof(AddProgrammeTaskLink),
            ResultType: typeof(ProgrammeTaskLink),
            AuthorisationType: typeof(AddProgrammeTaskLinkAuthorisation),
            ValidationType: typeof(AddProgrammeTaskLinkValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Both task ids must belong to the same project's programme."),

        new AiAction(
            Name: "remove_programme_task_link",
            Area: "Progress & programme",
            Description: "Removes a dependency link between two programme tasks. The tasks "
                + "themselves are untouched.",
            CommandType: typeof(RemoveProgrammeTaskLink),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveProgrammeTaskLinkAuthorisation),
            ValidationType: typeof(RemoveProgrammeTaskLinkValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "take_programme_baseline",
            Area: "Progress & programme",
            Description: "Snapshots every live programme task under a named baseline. Movement — "
                + "and the contemporaneous delay evidence behind NOD/EOT claims — is measured "
                + "against the latest baseline, so taking one deliberately RESETS that yardstick.",
            CommandType: typeof(TakeProgrammeBaseline),
            ResultType: typeof(ProgrammeBaseline),
            AuthorisationType: typeof(TakeProgrammeBaselineAuthorisation),
            ValidationType: typeof(TakeProgrammeBaselineValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — this resets the delay-evidence "
                + "yardstick. Fails if the programme has no tasks yet. takenByEmail is recorded on "
                + "the baseline; pass the acting user's portal email (the endpoint does not stamp "
                + "it)."),

        new AiAction(
            Name: "remove_programme_baseline",
            Area: "Progress & programme",
            Description: "Removes a programme baseline and its task snapshots permanently. The "
                + "previous baseline (if any) becomes the yardstick delay evidence is measured "
                + "against — like taking one, this deliberately resets that yardstick.",
            CommandType: typeof(RemoveProgrammeBaseline),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveProgrammeBaselineAuthorisation),
            ValidationType: typeof(RemoveProgrammeBaselineValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which baseline, by label, before calling. There is no "
                + "undo."),

        // ── Progress & programme (progress updates and reports) ───────────────────────────────

        new AiAction(
            Name: "update_progress_update",
            Area: "Progress & programme",
            Description: "Updates a progress update's title, description, work date or weather "
                + "record. Its photos are untouched.",
            CommandType: typeof(UpdateProgressUpdate),
            ResultType: typeof(ProgressUpdate),
            AuthorisationType: typeof(UpdateProgressUpdateAuthorisation),
            ValidationType: typeof(UpdateProgressUpdateValidation),
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Every field is written as posted — read the current update first and carry "
                + "forward what should not change."),

        new AiAction(
            Name: "delete_progress_update",
            Area: "Progress & programme",
            Description: "Deletes a progress update permanently, together with its photos and "
                + "their stored files, and removes it from any report selections. There is no "
                + "undo.",
            CommandType: typeof(DeleteProgressUpdate),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteProgressUpdateAuthorisation),
            ValidationType: null,
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which update, by title and date, before calling."),

        new AiAction(
            Name: "delete_progress_photo",
            Area: "Progress & programme",
            Description: "Deletes one photo from a progress update permanently, including its "
                + "stored file. There is no undo.",
            CommandType: typeof(DeleteProgressPhoto),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteProgressPhotoAuthorisation),
            ValidationType: null,
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user before calling."),

        new AiAction(
            Name: "create_progress_report",
            Area: "Progress & programme",
            Description: "Creates a client-facing progress report for a project from selected "
                + "progress updates (title, period, introduction, work completed, upcoming "
                + "works). Recorded as created by the signed-in user.",
            CommandType: typeof(CreateProgressReport),
            ResultType: typeof(ProgressReport),
            AuthorisationType: typeof(CreateProgressReportAuthorisation),
            ValidationType: typeof(CreateProgressReportValidation),
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "selectedUpdateIds are progress-update ids from the project's progress feed."),

        new AiAction(
            Name: "update_progress_report",
            Area: "Progress & programme",
            Description: "Updates a progress report's title, period, narrative sections and which "
                + "progress updates it includes.",
            CommandType: typeof(UpdateProgressReport),
            ResultType: typeof(ProgressReport),
            AuthorisationType: typeof(UpdateProgressReportAuthorisation),
            ValidationType: typeof(UpdateProgressReportValidation),
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Every field is written as posted — read the current report first and carry "
                + "forward what should not change."),

        new AiAction(
            Name: "delete_progress_report",
            Area: "Progress & programme",
            Description: "Deletes a progress report permanently. The underlying progress updates "
                + "and their photos are untouched and remain available for other reports.",
            CommandType: typeof(DeleteProgressReport),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteProgressReportAuthorisation),
            ValidationType: null,
            VisibleTo: ProgressRoles.Contributors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which report, by title, before calling."),

        // ── Drawings ──────────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "register_drawing",
            Area: "Drawings",
            Description: "Registers a new drawing on a project's drawing register (code and "
                + "title, optionally inside a folder). Revisions are uploaded separately through "
                + "the portal.",
            CommandType: typeof(RegisterDrawing),
            ResultType: typeof(Drawing),
            AuthorisationType: typeof(RegisterDrawingAuthorisation),
            ValidationType: typeof(RegisterDrawingValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "drawingFolderId is optional — omit it to register at the project root."),

        new AiAction(
            Name: "update_drawing_metadata",
            Area: "Drawings",
            Description: "Updates a drawing's code and title on the register. Its revisions and "
                + "files are untouched.",
            CommandType: typeof(UpdateDrawingMetadata),
            ResultType: typeof(Drawing),
            AuthorisationType: typeof(UpdateDrawingMetadataAuthorisation),
            ValidationType: typeof(UpdateDrawingMetadataValidation),
            VisibleTo: DrawingEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "approve_drawing_revision",
            Area: "Drawings",
            Description: "Approves a drawing revision — it becomes the drawing's single Approved "
                + "(latest) revision, EVERY other revision of that drawing is archived, and the "
                + "drawing's current approved label is set. Recorded as approved by the signed-in "
                + "user.",
            CommandType: typeof(ApproveDrawingRevision),
            ResultType: typeof(DrawingRevision),
            AuthorisationType: typeof(ApproveDrawingRevisionAuthorisation),
            ValidationType: typeof(ApproveDrawingRevisionValidation),
            VisibleTo: DrawingManagers,
            EmailStamps: new[] { "ApprovedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which revision, by drawing code and revision label, "
                + "before calling — the previous approved revision is archived."),

        new AiAction(
            Name: "set_drawing_revision_label",
            Area: "Drawings",
            Description: "Sets or clears a drawing revision's label (e.g. \"P3\", \"C1\").",
            CommandType: typeof(SetDrawingRevisionLabel),
            ResultType: typeof(DrawingRevision),
            AuthorisationType: typeof(SetDrawingRevisionLabelAuthorisation),
            ValidationType: typeof(SetDrawingRevisionLabelValidation),
            VisibleTo: DrawingEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A blank revisionLabel clears the label."),

        new AiAction(
            Name: "delete_drawing_revision",
            Area: "Drawings",
            Description: "Deletes one drawing revision permanently, including its stored file. "
                + "There is no undo.",
            CommandType: typeof(DeleteDrawingRevision),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDrawingRevisionAuthorisation),
            ValidationType: typeof(DeleteDrawingRevisionValidation),
            VisibleTo: DrawingManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which revision, by drawing code and label, before "
                + "calling."),

        new AiAction(
            Name: "delete_drawing",
            Area: "Drawings",
            Description: "Deletes a drawing permanently, together with ALL of its revisions, "
                + "their stored files and any issue records that referenced them. There is no "
                + "undo.",
            CommandType: typeof(DeleteDrawing),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDrawingAuthorisation),
            ValidationType: typeof(DeleteDrawingValidation),
            VisibleTo: DrawingManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which drawing, by code and title, before calling."),

        new AiAction(
            Name: "create_drawing_folder",
            Area: "Drawings",
            Description: "Creates a folder on a project's drawing register, optionally inside a "
                + "parent folder.",
            CommandType: typeof(CreateDrawingFolder),
            ResultType: typeof(DrawingFolder),
            AuthorisationType: typeof(CreateDrawingFolderAuthorisation),
            ValidationType: typeof(CreateDrawingFolderValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A duplicate name among siblings is refused."),

        new AiAction(
            Name: "rename_drawing_folder",
            Area: "Drawings",
            Description: "Renames a drawing folder. Its contents are untouched.",
            CommandType: typeof(RenameDrawingFolder),
            ResultType: typeof(DrawingFolder),
            AuthorisationType: typeof(RenameDrawingFolderAuthorisation),
            ValidationType: typeof(RenameDrawingFolderValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "delete_drawing_folder",
            Area: "Drawings",
            Description: "Deletes a drawing folder from the register. Business-rule refusals "
                + "(e.g. a folder that is not empty) come back as errors rather than deleting "
                + "contents.",
            CommandType: typeof(DeleteDrawingFolder),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDrawingFolderAuthorisation),
            ValidationType: typeof(DeleteDrawingFolderValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which folder, by name, before calling."),

        new AiAction(
            Name: "move_drawing_to_folder",
            Area: "Drawings",
            Description: "Moves a drawing into a folder on the same project's register, or to the "
                + "register root with a null drawingFolderId.",
            CommandType: typeof(MoveDrawingToFolder),
            ResultType: typeof(Drawing),
            AuthorisationType: typeof(MoveDrawingToFolderAuthorisation),
            ValidationType: typeof(MoveDrawingToFolderValidation),
            VisibleTo: DrawingRegisterCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Cross-project moves are refused."),

        // ── Closeout & defects ────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "raise_defect",
            Area: "Closeout & defects",
            Description: "Raises a defect on a project (description, location, assignee email). "
                + "It is numbered from the global defect sequence (DEF-####) and opens in Open "
                + "status.",
            CommandType: typeof(RaiseDefect),
            ResultType: typeof(Defect),
            AuthorisationType: typeof(RaiseDefectAuthorisation),
            ValidationType: typeof(RaiseDefectValidation),
            VisibleTo: DefectRaisers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "assignedToEmail is who should fix it — usually a subcontractor's portal "
                + "email, not the signed-in user."),

        new AiAction(
            Name: "create_defect_from_message",
            Area: "Closeout & defects",
            Description: "Raises a defect from a mailbox message (triage pathway) and tags the "
                + "originating email to it — same numbering and Open status as a manually raised "
                + "defect, whichever door it came in through.",
            CommandType: typeof(CreateDefectFromMessage),
            ResultType: typeof(Defect),
            AuthorisationType: typeof(CreateDefectFromMessageAuthorisation),
            ValidationType: typeof(CreateDefectFromMessageValidation),
            VisibleTo: SiteTeamManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue. An email already "
                + "tagged to another pathway is refused unless allowCrossPathway is true."),

        new AiAction(
            Name: "update_defect",
            Area: "Closeout & defects",
            Description: "Updates a defect's description, location, assignee and status. Moving "
                + "it to Resolved or Verified for the first time stamps the resolution time.",
            CommandType: typeof(UpdateDefect),
            ResultType: typeof(Defect),
            AuthorisationType: typeof(UpdateDefectAuthorisation),
            ValidationType: typeof(UpdateDefectValidation),
            VisibleTo: SiteTeamManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Every field is written as posted — read the current defect (list_defects) "
                + "first and carry forward what should not change. Confirm with the user before "
                + "marking a defect Resolved or Verified."),

        new AiAction(
            Name: "agree_settlement",
            Area: "Closeout & defects",
            Description: "Records (or overwrites) a project's agreed final-account settlement — "
                + "final contract value, final cost, final margin and whether the client has "
                + "signed. One settlement record per project; calling again replaces the figures "
                + "and re-stamps the agreement time.",
            CommandType: typeof(AgreeSettlement),
            ResultType: typeof(SettlementRecord),
            AuthorisationType: typeof(AgreeSettlementAuthorisation),
            ValidationType: typeof(AgreeSettlementValidation),
            VisibleTo: CloseoutDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A financial sign-off — confirm the figures with the user before calling."),

        new AiAction(
            Name: "agree_vat_analysis",
            Area: "Closeout & defects",
            Description: "Records (or overwrites) a project's agreed VAT analysis — zero-rated "
                + "and standard-rated amounts, notes, and client/architect confirmation flags. "
                + "One analysis per project; calling again replaces it.",
            CommandType: typeof(AgreeVatAnalysis),
            ResultType: typeof(VatAnalysis),
            AuthorisationType: typeof(AgreeVatAnalysisAuthorisation),
            ValidationType: typeof(AgreeVatAnalysisValidation),
            VisibleTo: CloseoutDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A financial sign-off — confirm the figures with the user before calling."),

        new AiAction(
            Name: "release_retention",
            Area: "Closeout & defects",
            Description: "Records a retention release for a project — the amount, the release "
                + "time (now) and whether it is published downstream. Each call adds a new "
                + "release record.",
            CommandType: typeof(ReleaseRetention),
            ResultType: typeof(RetentionRelease),
            AuthorisationType: typeof(ReleaseRetentionAuthorisation),
            ValidationType: typeof(ReleaseRetentionValidation),
            VisibleTo: CloseoutDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A financial action — confirm the amount with the user before calling. "
                + "Distinct from confirm_retention_release, which acts on the commercial "
                + "retention schedule."),
    };

    // Skipped: AddTodoItem — already a first-class AiWriteTools tool (add_todo).
    // Skipped: AddGeneralTodoItem — already a first-class AiWriteTools tool (add_todo).
    // Skipped: UpdateTodoItem — already dispatched by AiWriteTools (complete_todo).
    // Skipped: LogTodoProgress — already a first-class AiWriteTools tool (log_todo_progress).
    // Skipped: CreateProgressUpdate — multipart/form-data photo upload; cannot fit the pattern.
    // Skipped: AddProgressPhotos — multipart/form-data photo upload; cannot fit the pattern.
    // Skipped: UploadDrawingRevision — multipart/form-data file upload; cannot fit the pattern.
}
