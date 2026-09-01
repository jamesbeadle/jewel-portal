using Jewel.JPMS.Api.Features.Closeout.Commands;
using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SiteAndProgressActions
{
    private static IEnumerable<AiAction> ProgressProgrammeActions() => new AiAction[]
    {
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

    };
}
