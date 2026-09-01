using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Features.Triage.Workspace;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ---- Project auto-match ----
    // A simple lower-case search of the email chain for a project's name: when exactly one live
    // project's name appears verbatim (case-insensitive) in the selected email's subject, body or
    // thread, the project pickers are pre-filled with it. The triager still sees — and can change —
    // the choice; an ambiguous chain (two project names in one thread) pre-fills nothing, and a
    // choice already made is never overridden.
    private async Task TryPrefillProjectFromEmailAsync()
    {
        if (view != QueueView.Active || selected is null) return;
        if (!string.IsNullOrWhiteSpace(triageProjectId)) return;

        var haystack = BuildEmailSearchText();
        if (haystack.Length == 0) return;

        // Live projects only (the pickers hide completed ones by default), and names under four
        // characters are skipped — too short to be an honest match rather than a coincidence.
        var matches = AllProjects
            .Where(project => project.Stage != ProjectStage.Completed)
            .Where(project => project.Name.Trim() is { Length: >= 4 } name
                && haystack.Contains(name.ToLowerInvariant(), StringComparison.Ordinal))
            .ToList();
        if (matches.Count != 1) return;

        triageProjectId = matches[0].ProjectId;
        projectAutoMatched = true;
        // The link panel shows records for its chosen project, so the pre-fill loads them too —
        // otherwise it would claim "no records on this project yet" without having looked.
        await LoadLinkRecordsAsync();
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

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:0.#} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes} B";
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
        return side is TriagePathway.Client or TriagePathway.Subcontractor ? PathwayLabel(side.Value) : null;
    }

    // "Reply in thread": the reply written here is staged as an Outlook draft on the email
    // (projects mailbox, thread quoted behind it) AND becomes the description of a General request
    // created from it in the background — one write-up answers the email and papers the request, so
    // the email is triaged by the act of replying. The outcome (request + draft weblink) is kept
    // for the success banner; the pre-filled draft is reviewed and sent from Outlook itself.
}
