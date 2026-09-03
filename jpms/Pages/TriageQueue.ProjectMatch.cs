using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Features.Triage.Workspace;

using Jewel.JPMS.Features.Triage.Queue;
namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ---- Project auto-match ----
    // Two sources, in order of trust. (1) The thread's own record tags: an earlier email in the
    // chain already filed to "JPMS/JBB-2026-002-RFI-017" names its project outright, so a reply
    // lands on that project without reading a word of it. (2) Failing that, the project's NAME as
    // a whole word in the selected email's subject, body or thread (ProjectNameMatch — a lead
    // project called "Test" used to match "latest" and poison the whole chain, 2026-08-28). Either
    // way the rule is the same: exactly one project pre-fills; an ambiguous answer (two projects'
    // tags, two names in one thread) pre-fills nothing. The triager still sees — and can change —
    // the choice, and a choice already made is never overridden.
    private async Task TryPrefillProjectFromEmailAsync()
    {
        if (view != QueueView.Active || selected is null) return;
        if (!string.IsNullOrWhiteSpace(triageProjectId)) return;

        var projectId = await ProjectFromThreadTagsAsync(selected) ?? ProjectFromEmailText();
        if (projectId is null) return;

        triageProjectId = projectId;
        projectAutoMatched = true;
        // The link panel shows records for its chosen project, so the pre-fill loads them too —
        // otherwise it would claim "no records on this project yet" without having looked.
        await LoadLinkRecordsAsync();
    }

    // The project the chain's existing record tags name, or null when they name none or more than
    // one. Project-referenced stems ("JBB-2026-002-RFI-017", the programme bucket "SCH-JBB-2026-002")
    // are read off the project list here; any other record stems (to-dos, bid packages…) are
    // resolved the way the tag chips are (ResolveRecordTags) — a live read that can fail, and a
    // failure here is simply "no opinion", never an error toast on opening an email.
    private async Task<string?> ProjectFromThreadTagsAsync(MailboxMessage anchor)
    {
        var stems = thread.Prepend(anchor)
            .SelectMany(member => member.Categories)
            .Select(TriageEmailDisplay.TagLabel)
            .Where(stem => !string.IsNullOrWhiteSpace(stem) && !IsWorkflowTag(stem))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (stems.Count == 0) return null;

        var projectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unresolved = new List<string>();
        foreach (var stem in stems)
        {
            var byReference = AllProjects.FirstOrDefault(project => StemBelongsTo(stem, project));
            if (byReference is not null) projectIds.Add(byReference.ProjectId);
            else unresolved.Add(stem);
        }

        if (unresolved.Count > 0)
        {
            try
            {
                var records = await Queries.AskAsync(new ResolveRecordTags(unresolved), CancellationToken.None);
                // The read is live: if the triager has moved on to another email meanwhile, this
                // answer belongs to the old one and must not land on the new.
                if (!ReferenceEquals(selected, anchor)) return null;
                foreach (var record in records)
                    if (!string.IsNullOrWhiteSpace(record.ProjectId)) projectIds.Add(record.ProjectId);
            }
            catch
            {
                // No opinion from the tags — fall through to whatever the references alone said.
            }
        }

        return projectIds.Count == 1 ? projectIds.First() : null;
    }

    // "JBB-2026-002-RFI-017" and "SCH-JBB-2026-002" both belong to project JBB-2026-002. The
    // reference must be followed by the end of the stem or a separator, so JBB-2026-01 can never
    // claim JBB-2026-012's records.
    private static bool StemBelongsTo(string stem, Project project)
    {
        var reference = project.Reference?.Trim();
        if (string.IsNullOrEmpty(reference)) return false;
        if (stem.StartsWith("SCH-", StringComparison.OrdinalIgnoreCase))
            return string.Equals(stem["SCH-".Length..], reference, StringComparison.OrdinalIgnoreCase);
        return stem.StartsWith(reference, StringComparison.OrdinalIgnoreCase)
            && (stem.Length == reference.Length || stem[reference.Length] == '-');
    }

    // Tags that route or mark an email rather than name a record — never a project clue.
    private static bool IsWorkflowTag(string stem) => stem.ToLowerInvariant() is
        "discarded" or "replied" or "admin"
        or "client" or "subcontractor" or "supplier" or "internal"
        or "intcomms" or "subcomms" or "supcomms";

    // The one live project whose name stands as a whole word somewhere in the chain, else null.
    // Live projects only (the pickers hide completed ones by default); names under four characters
    // are skipped by ProjectNameMatch — too short to be an honest match rather than a coincidence.
    private string? ProjectFromEmailText()
    {
        var haystack = BuildEmailSearchText();
        if (haystack.Length == 0) return null;
        var matches = AllProjects
            .Where(project => project.Stage != ProjectStage.Completed)
            .Where(project => ProjectNameMatch.MentionsName(haystack, project.Name))
            .ToList();
        return matches.Count == 1 ? matches[0].ProjectId : null;
    }

    // Everything searchable about the selected email's chain, joined and lower-cased once: subject,
    // preview, the fetched body (tags stripped when HTML) and every thread member's subject/preview.
    private string BuildEmailSearchText()
    {
        var parts = new List<string?> { selected?.Subject, selected?.BodyPreview };
        if (detail is not null)
            parts.Add(detail.BodyIsHtml ? StripHtml(detail.BodyHtml) : detail.BodyHtml);
        foreach (var member in thread)
        {
            parts.Add(member.Subject);
            parts.Add(member.BodyPreview);
        }
        return string.Join("\n", parts.Where(part => !string.IsNullOrWhiteSpace(part))).ToLowerInvariant();
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html ?? "", "<[^>]*>", " ");

    private static string ThreadRowClass(bool current)
    {
        var baseClass = "w-full text-left rounded-lg border px-3 py-2 transition";
        return current
            ? $"{baseClass} border-accent bg-surface"
            : $"{baseClass} border-line hover:border-line-strong hover:bg-surface";
    }


    // Choose the pathway for a not-yet-filed thread (staging from a pathway pane). Staged picks
    // deliberately SURVIVE the switch — the modal shows its running total across every tab, and a
    // genuine cross-pathway combination simply files the thread under both at apply (the confirm
    // was retired 2026-08-28). A thread that already carries a pathway ignores this — its routing
    // was decided at first filing.
    private void SetPathway(TriagePathway next)
    {
        if (FixedPathway is not null || pathway == next) return;
        pathway = next;
        actionError = null;
    }

    private async Task OnLinkRecordTypeChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<RecordType>(e.Value?.ToString(), out var type)) linkRecordType = type;
        linkRecordId = "";
        // pickedRecords deliberately survive a type switch — that's how one email links to
        // records of several types in one apply.
        await LoadLinkRecordsAsync();
    }

    // One landing for the email's project decision: the triage bar's ProjectSelect hands the id
    // straight in; the Tagged view's plain <select> still speaks ChangeEventArgs and forwards.
    private async Task OnTriageProjectPicked(string projectId)
    {
        triageProjectId = projectId;
        projectAutoMatched = false; // an explicit choice replaces the guess
        linkRecordId = "";
        pickedRecords.Clear();
        await LoadLinkRecordsAsync();
    }

    private async Task OnTriageProjectChanged(ChangeEventArgs e) =>
        await OnTriageProjectPicked(e.Value?.ToString() ?? "");

    // Load the chosen project's records of the chosen type for the picker. Record-agnostic: the same
    // call backs both the Link panel and the Tagged tab's "link to another record" control.
    private async Task LoadLinkRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(triageProjectId))
        {
            linkRecords = Array.Empty<LinkableRecord>();
            return;
        }
        try
        {
            linkRecordsLoading = true;
            linkRecordId = "";
            pickedRecords.Clear();
            StateHasChanged(); // show the loading state while the fetch is in flight
            linkRecords = await Intake.ListLinkableRecordsAsync(triageProjectId, linkRecordType);
        }
        catch
        {
            linkRecords = Array.Empty<LinkableRecord>();
        }
        finally
        {
            linkRecordsLoading = false;
        }
    }

    // The first record type the current context offers (the chosen pathway on the queue, the thread's
    // bucket on the Tagged tab) — what the link picker resets to, so a type from another pathway can
    // never survive a pathway or selection change.
    private RecordType DefaultLinkRecordType => view == QueueView.Tagged
        ? TaggedLinkTypeOptions[0]
        : (QueueLinkTypeOptions.Count > 0 ? QueueLinkTypeOptions[0] : RecordType.Request);

    // Clear the link picker back to the current context's defaults after a selection or pathway
    // changes, or a link action completes.
    // Clears the record picks and pool — NOT the project: the project is the email's own global
    // step and survives pathway/action switches.
    private void ResetLinkState()
    {
        linkRecordType = DefaultLinkRecordType;
        linkRecordId = "";
        pickedRecords.Clear();
        linkRecords = Array.Empty<LinkableRecord>();
    }

    // The pathway label sent with a link command. Only pathway-neutral COST-CENTRE links carry one —
    // the record type implies the pathway everywhere else, and a Todo link must stay neutral (sending
    // a pathway with it would file the thread, which a to-do never does). On the queue it is the
    // triager's selection; on the Tagged tab it is the thread's own side. Internal never applies —
    // cost-centre mail is valuation-side (Client) or subcontract-side (Subcontractor) only.
    private string? CostCentrePathwayFor(LinkableRecord record)
    {
        if (record.Type != RecordType.CostCentre) return null;
        var side = view == QueueView.Tagged ? FixedPathway : pathway;
        return side is TriagePathway.Client or TriagePathway.Subcontractor ? TriagePathways.Label(side.Value) : null;
    }

    // "Reply in thread": the reply written here is staged as an Outlook draft on the email
    // (projects mailbox, thread quoted behind it) AND becomes the description of a General request
    // created from it in the background — one write-up answers the email and papers the request, so
    // the email is triaged by the act of replying. The outcome (request + draft weblink) is kept
    // for the success banner; the pre-filled draft is reviewed and sent from Outlook itself.
}
