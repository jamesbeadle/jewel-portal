using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Pages;

public partial class ProjectProgramme
{
    // ---- Relevant Events: expand to the full email, and reply in the email's thread ----

    // Emails render their short preview by default; expanding fetches the FULL body (with the quoted
    // thread + attachment names) live from the mailbox, cached per message for the page's life.
    private readonly HashSet<string> expandedEmails = new();
    private readonly Dictionary<string, MailboxMessageDetail?> emailDetails = new();

    private string? replyForId;      // email whose reply composer is open
    private string replyBody = "";
    private bool replyBusy;
    private string? replyError;
    private string? replyDraftForId; // email whose staged-draft confirmation is showing
    private ProgrammeReplyDraft? replyDraft;

    // Mirrors PrepareProgrammeReplyDraftEndpoint server-side (directors, project managers, site
    // managers; admins carry every role server-side).
    private bool CanDraftReply => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager);

    private async Task ToggleEmailAsync(MailboxMessage email)
    {
        if (!expandedEmails.Add(email.Id))
        {
            expandedEmails.Remove(email.Id);
            return;
        }

        if (emailDetails.ContainsKey(email.Id)) return;

        try
        {
            emailDetails[email.Id] = await Queries.AskAsync(
                new GetProgrammeEmailDetail(ProjectId, email.Id, email.InternetMessageId), CancellationToken.None);
        }
        catch
        {
            // Fetch failed — fall back to the preview body inside the expanded pane.
            emailDetails[email.Id] = new MailboxMessageDetail(email.Id, "", false, Array.Empty<IntakeAttachment>());
        }
    }

    private void ToggleReply(MailboxMessage email)
    {
        if (replyForId == email.Id)
        {
            replyForId = null;
            replyError = null;
            return;
        }
        replyForId = email.Id;
        replyBody = "";
        replyError = null;
        replyDraft = null;
        replyDraftForId = null;
    }

    private void OnReplyInput(ChangeEventArgs e) => replyBody = e.Value?.ToString() ?? "";

    private async Task CreateReplyDraftAsync(MailboxMessage email)
    {
        if (replyBusy || string.IsNullOrWhiteSpace(replyBody)) return;
        replyBusy = true;
        replyError = null;
        try
        {
            replyDraft = await Commands.SendAsync(new PrepareProgrammeReplyDraft(
                ProjectId, email.Id, replyBody.Trim(), email.InternetMessageId), CancellationToken.None);
            replyDraftForId = email.Id;
            replyForId = null;
            replyBody = "";
        }
        catch (CommandFailedException ex)
        {
            // Validation answers (empty reply, vanished email) come back as 400s with the server's
            // own sentence — shown here next to the composer, per the error-reporting convention.
            replyError = ex.Message;
        }
        catch
        {
            replyError = "Couldn't create the reply draft. Check the mailbox connection and try again.";
        }
        finally
        {
            replyBusy = false;
        }
    }

    private async Task RaiseNodAsync()
    {
        if (claimsBusy || string.IsNullOrWhiteSpace(nodTitle)) return;
        claimsBusy = true;
        claimsError = null;
        try
        {
            await RequestRegister.RaiseAsync(new RaiseRequest(
                ProjectId, RequestType.NoticeOfDelay, Reference: "", nodTitle.Trim(), nodDescription.Trim(),
                Value: null, RaisedByEmail: Auth.CurrentUser!.Email));
            nodTitle = "";
            nodDescription = "";
            openForm = ClaimForm.None;
        }
        catch
        {
            claimsError = "Couldn't raise the Notice of Delay. Please try again.";
        }
        finally
        {
            claimsBusy = false;
        }
    }

    private async Task RaiseEotAsync()
    {
        if (claimsBusy || string.IsNullOrWhiteSpace(eotTitle)) return;
        claimsBusy = true;
        claimsError = null;
        try
        {
            await RequestRegister.RaiseAsync(new RaiseRequest(
                ProjectId, RequestType.ExtensionOfTime, Reference: "", eotTitle.Trim(), eotDescription.Trim(),
                Value: null, RaisedByEmail: Auth.CurrentUser!.Email,
                RelatedNodRequestId: string.IsNullOrWhiteSpace(eotRelatedNodId) ? null : eotRelatedNodId));
            eotTitle = "";
            eotDescription = "";
            eotRelatedNodId = "";
            openForm = ClaimForm.None;
        }
        catch
        {
            claimsError = "Couldn't raise the Extension of Time. Please try again.";
        }
        finally
        {
            claimsBusy = false;
        }
    }

    private async Task RecordLadAsync()
    {
        if (claimsBusy || string.IsNullOrWhiteSpace(ladTitle)) return;
        claimsBusy = true;
        claimsError = null;
        try
        {
            await Commands.SendAsync(new AddLadClaim(
                ProjectId, ladTitle.Trim(), ladDescription.Trim(),
                // Date-only values: pin to UTC midnight so the stored instant is timezone-stable
                // (a local offset would shift the date in UTC-based comparisons and reports).
                PeriodFrom: ladPeriodFrom is { } from ? new DateTimeOffset(from.Date, TimeSpan.Zero) : null,
                PeriodTo: ladPeriodTo is { } to ? new DateTimeOffset(to.Date, TimeSpan.Zero) : null,
                DaysClaimed: ladDaysClaimed,
                RatePerWeek: ladRatePerWeek,
                Amount: ladAmount), CancellationToken.None);
            ladTitle = "";
            ladDescription = "";
            ladPeriodFrom = null;
            ladPeriodTo = null;
            ladDaysClaimed = 0;
            ladRatePerWeek = 0m;
            ladAmount = 0m;
            openForm = ClaimForm.None;
            await LoadLadsAsync();
        }
        catch
        {
            claimsError = "Couldn't record the LADs claim. Please try again.";
        }
        finally
        {
            claimsBusy = false;
        }
    }

    // The reference of the NOD an EOT arises from, resolved against the project's NODs — null when
    // the EOT stands alone (or the NOD has since been deleted).
    private string? RelatedNodReference(Request eot) =>
        string.IsNullOrWhiteSpace(eot.RelatedNodRequestId)
            ? null
            : Nods.FirstOrDefault(n => n.RequestId == eot.RelatedNodRequestId)?.Reference;

    private static string ClaimPeriod(LadClaim lad) =>
        (lad.PeriodFrom, lad.PeriodTo) switch
        {
            ({ } from, { } to) => $"{from.LocalDateTime:d MMM yyyy} – {to.LocalDateTime:d MMM yyyy}",
            ({ } from, null)   => $"from {from.LocalDateTime:d MMM yyyy}",
            (null, { } to)     => $"to {to.LocalDateTime:d MMM yyyy}",
            _                  => "Period not recorded"
        };


    // Status chip helpers for the Critical Path RFIs view — mirrors the request detail page's
    // colouring so an RFI reads the same on both sides.
    private static string RfiStatusLabel(RequestStatus status) => status.DisplayName();

    private static string RfiStatusClass(RequestStatus status) => status switch
    {
        RequestStatus.NeedsAction    => "bg-amber-500/10 text-amber-600",
        RequestStatus.Open           => "bg-blue-500/10 text-blue-600",
        RequestStatus.NeedsVariation => "bg-violet-500/10 text-violet-600",
        RequestStatus.Closed         => "bg-surface-raised border border-line text-content-muted",
        _                            => "bg-surface-raised border border-line text-content-muted"
    };

    private string SubTabClass(SubView tab)
    {
        var baseClass = "px-3 py-2 text-sm font-medium border-b-2 -mb-px transition inline-flex items-center";
        return view == tab
            ? $"{baseClass} border-accent text-content"
            : $"{baseClass} border-transparent text-content-muted hover:text-content";
    }
}
