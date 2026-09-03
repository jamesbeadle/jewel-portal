using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Features.Triage.Workspace;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ---- Preview an attachment without leaving triage ----
    // Same previewable set as the drawing viewer: PDFs (the in-app viewer) and images; everything
    // else gets a Download link only. Bytes are proxied through the API on demand
    // (mailbox/message/attachment) — nothing is stored in JPMS by previewing. The document opens
    // in the Preview pane on the window OPPOSITE the email, the same route as a record's
    // documents, so email and attachment read side by side. The URLs are baked at click time —
    // the preview outlives the selection that opened it.



    private void OpenEmailAttachmentPreview(IntakeAttachment attachment)
    {
        // Type OR .pdf name — a sender's system may label a real PDF application/octet-stream.
        var isPdf = TriageEmailDisplay.IsPdf(attachment);
        workspace.OpenPreview(
            new PreviewRequest(attachment.Name, TriageEmailDisplay.AttachmentUrl(selected?.Id ?? "", attachment, inline: true),
                TriageEmailDisplay.AttachmentUrl(selected?.Id ?? "", attachment, inline: false), isPdf),
            anchor: PanelKind.Email);
    }

    // Ticked per attachment on the open email and staged like every other triage draft — the
    // email's Apply copies the files mailbox → Document Triage server-side. Like the
    // save-to-drawings form this replaced (2026-08-12), it does NOT consume the email: the
    // message keeps its place in triage — only the files are copied out. Choosing each file's
    // DESTINATION happens in Document Triage itself, but the PROJECT is decided here, where
    // the email says which job it is: Apply requires one while attachments are ticked
    // (decision 2026-08-28 — a projectless file in the queue is as good as discarded).

    private readonly List<string> stagedDocControlIds = new();

    private void ToggleDocControl(string attachmentId, bool ticked)
    {
        if (ticked)
        {
            if (!stagedDocControlIds.Contains(attachmentId)) stagedDocControlIds.Add(attachmentId);
        }
        else
        {
            stagedDocControlIds.Remove(attachmentId);
        }
    }

    // Runs a Queue-tab action (assign / create / discard), then refreshes the inbox and clears the
    // selection — the message has left the Inbox, so it drops out of the live read. `label` captions
    // the detail-pane spinner while the action is in flight.
    private async Task RunAction(string label, Func<Task> action)
    {
        actionError = null;
        try
        {
            busyLabel = label;
            busy = true;
            await action();
            // The queue and the recently-triaged panel move together: the action that consumed
            // this email is the newest row of the panel. The reload stays on the current page
            // (in-place) so emails the triager skipped don't come round again after every action.
            await Task.WhenAll(ReloadQueueInPlaceAsync(), LoadRecentTriageAsync());
            selected = null;
            detail = null;
            detailLoading = false;
            discardArmed = false;
            stagedCreate = null;
            relevantEventStaged = null;
            triageEntireThread = null;
            useThreadTags = null;
            pickedRecords.Clear();
            ReturnWorkspaceToQueue();
        }
        catch (CommandFailedException ex)
        {
            // e.g. the reference is already in use on this project.
            actionError = ex.Message;
        }
        catch
        {
            actionError = "That action didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    // Runs a Tagged-tab action (add a tag, remove a tag), then refreshes the tagged list and clears the
    // selection — the email's tag set has changed, so re-read it live.
    private async Task RunTaggedAction(string label, Func<Task> action)
    {
        actionError = null;
        try
        {
            busyLabel = label;
            busy = true;
            await action();
            await ReloadTaggedInPlaceAsync();
            selected = null;
            detail = null;
            detailLoading = false;
            ResetLinkState();
            ReturnWorkspaceToQueue();
        }
        catch
        {
            actionError = "That action didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    // Add another workflow tag by linking this already-tagged email to a second record (so it feeds
    // more than one record). Reuses the same generic link command as the queue's "Link to existing",
    // but NOT RunTaggedAction — a link failure belongs next to the picker, not in the toast. Any
    // crossing — the former client wall included (removed 2026-08-21) — simply files the thread
    // under both: AllowCrossPathway: true, since the picker's own heads-up already says where the
    // link files the thread (the confirm was retired 2026-08-28).
    private async Task DoAddTagLink()
    {
        if (selected is null || busy || string.IsNullOrWhiteSpace(linkRecordId)) return;
        // The type to link as is the picked RECORD's own type, not the dropdown's (the Scheduling
        // picker lists NOD/EOT/LAD claims documents alongside the bucket — see DoApplyAll).
        var record = linkRecords.FirstOrDefault(r => r.RecordId == linkRecordId);
        var recordType = record?.Type ?? linkRecordType;
        actionError = null;
        try
        {
            busyLabel = "Linking";
            busy = true;
            await Intake.LinkMessageToRecordAsync(
                selected.Id, selected.InternetMessageId, recordType, linkRecordId,
                pathway: record is null ? null : CostCentrePathwayFor(record),
                allowCrossPathway: true);
            await ReloadTaggedInPlaceAsync();
            selected = null;
            detail = null;
            detailLoading = false;
            ResetLinkState();
            ReturnWorkspaceToQueue();
        }
        catch (CommandFailedException ex)
        {
            actionError = ex.Message;
        }
        catch
        {
            actionError = "That action didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    private async Task DoRemoveTag(string tag)
    {
        if (selected is null || busy) return;
        await RunTaggedAction("Removing tag", async () => await Intake.RemoveTagFromMessageAsync(selected.Id, selected.InternetMessageId, tag));
    }

    private void OnTaggedRecordChanged(ChangeEventArgs e) => linkRecordId = e.Value?.ToString() ?? "";


    // Every project the signed-in user can see, completed ones included. Use this for LOOKING A
    // PROJECT UP BY ID (a stored id can point at a completed project whatever the toggle says);
    // use ProjectOptionsFor for anything a user picks from.
    private IReadOnlyList<Project> AllProjects =>
        Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>();

    // Completed projects are hidden from every picker on this page by default: triage routes live
    // Completed projects follow the per-user ProjectStageFilter toggle (the same one the side-nav
    // switcher uses — decision 2026-08-03) rather than a page-local checkbox: one preference,
    // honoured everywhere. The picker keeps an already-chosen completed project visible so the
    // bound <select> never points at a missing option.
    private IReadOnlyList<Project> ProjectOptionsFor(string? selectedProjectId) =>
        AllProjects
            .Where(project =>
                StageFilter.IncludeCompleted
                || project.Stage != ProjectStage.Completed
                || (!string.IsNullOrWhiteSpace(selectedProjectId)
                    && string.Equals(project.ProjectId, selectedProjectId, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    // The loaded records for the chosen type + project (empty until both are chosen and the load runs).
    private IReadOnlyList<LinkableRecord> ProjectRecords() => linkRecords;

    // Records on the chosen project whose reference or title overlaps the email subject — surfaced
    // first so a duplicate record isn't created for something already being tracked. Type-agnostic.
    private List<LinkableRecord> DuplicateCandidates()
    {
        var subject = selected?.Subject ?? "";
        var tokens = Tokenise(subject);
        return ProjectRecords()
            .Select(r => (r, score: Overlap(r, subject, tokens)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Select(x => x.r)
            .ToList();
    }

    private static int Overlap(LinkableRecord record, string subject, HashSet<string> subjectTokens)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(record.Reference) &&
            subject.Contains(record.Reference, StringComparison.OrdinalIgnoreCase))
            score += 10;
        foreach (var token in Tokenise(record.Title))
            if (subjectTokens.Contains(token)) score++;
        return score;
    }

    private static HashSet<string> Tokenise(string text) =>
        text.Split(new[] { ' ', '\t', '\n', '\r', '-', '_', '.', ',', ':', ';', '[', ']', '(', ')', '/' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 3)
            .ToHashSet();









    private string ProjectLabelFor(string projectId) =>
        AllProjects.FirstOrDefault(project => project.ProjectId == projectId)?.Name ?? "the chosen project";





    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // The to-do rows' assignee chips hold each pick as its TodoAssigneePicker value — a role
    // ("3"), optionally pinned to a person ("3|jane@…"). An empty list means unassigned; the
    // server raises one item per assignee in the list.
    private static IReadOnlyList<TodoAssignee> ParseTodoAssignees(IEnumerable<string> values) =>
        values
            .Select(TodoAssigneePicker.Parse)
            .Where(assignee => assignee is not null)
            .Select(assignee => assignee!)
            .Distinct()
            .ToList();

    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    // Date-only picker value ("yyyy-MM-dd") → a UTC date, matching how the manual work-order
    // modal and the other date pickers send dates — the purchase order prints a date, not a moment.
    private static DateTimeOffset? AsUtcDate(string value) =>
        DateTime.TryParse(value, out var parsed)
            ? new DateTimeOffset(DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc))
            : null;

}
