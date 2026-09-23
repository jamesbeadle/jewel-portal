using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Answers;
using Jewel.JPMS.Api.Features.Forms.Filing;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

public sealed partial class PublicFormService
{
    /// <summary>
    /// A form sent (api/forms-intake.js op:'submit'). Checked again here, so an old page cannot send
    /// what the form would refuse; sent twice — a retry after a dropped signal — it answers with the
    /// first receipt. The link is spent as the LAST step, never on opening, so a man who starts the
    /// form and comes back later still gets in. The hourly ceiling is for the open address only: five
    /// starters doing their packs on the office Wi-Fi are one address and are not strangers.
    /// </summary>
    public async Task<PublicFormReceipt> SubmitAsync(
        string slug, PublicFormSubmission posted, string clientHash, CancellationToken cancellationToken)
    {
        var form = Known(slug);
        CheckSession(posted.SessionId);
        var alreadySent = await AlreadySentAsync(posted.SessionId, form.Slug, cancellationToken);
        if (alreadySent is not null) return alreadySent;
        var link = await LinkOnSendingAsync(form.Slug, posted, cancellationToken);
        if (link is null) await CheckSubmissionLimitAsync(clientHash, cancellationToken);
        var answers = FormAnswerCleaning.Kept(form, posted.Answers);
        var uploads = await PostedUploadsAsync(posted, form.Slug, cancellationToken);
        var problem = FormAnswerRules.FirstProblem(form, answers, FileCounts(uploads));
        if (problem is not null) throw new PublicFormRefusal(problem);
        var submission = await RecordAsync(form, answers, uploads, link, posted.SessionId, clientHash, cancellationToken);
        await AnnounceAsync(form, submission, answers, uploads);
        return new PublicFormReceipt(submission.FormSubmissionId, submission.IsVerifiedLink);
    }

    private async Task<ResolvedLink?> LinkOnSendingAsync(string formSlug, PublicFormSubmission posted, CancellationToken cancellationToken)
    {
        var hasALink = !string.IsNullOrEmpty(posted.InviteToken) || !string.IsNullOrEmpty(posted.PackToken);
        if (!hasALink) return null;
        var link = await ResolveAsync(formSlug, posted.InviteToken, posted.PackToken, cancellationToken);
        var used = FormWording.DeadLink(FormLinkProblem.Used);
        if (link.Problem == FormLinkProblem.Used) throw new PublicFormRefusal($"{used.Heading}. {used.Body}");
        return FormLinkResolution.IsHonouredOnSending(link) ? link : null;
    }

    private async Task<List<FormUploadEntity>> PostedUploadsAsync(
        PublicFormSubmission posted, string formSlug, CancellationToken cancellationToken)
    {
        var postedIds = posted.Uploads.Values.SelectMany(ids => ids ?? Array.Empty<string>()).Distinct().Take(PublicFormLimits.MostFilesPerSession).ToList();
        return await context.FormUploads
            .Where(row => postedIds.Contains(row.FormUploadId) && row.SessionId == posted.SessionId && row.FormSlug == formSlug
                && row.FormSubmissionId == null && row.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    private static Dictionary<string, int> FileCounts(IEnumerable<FormUploadEntity> uploads) =>
        uploads.GroupBy(upload => upload.QuestionKey).ToDictionary(group => group.Key, group => group.Count());

    private async Task<FormSubmissionEntity> RecordAsync(
        FormDefinition form, Dictionary<string, string> answers, List<FormUploadEntity> uploads,
        ResolvedLink? link, string sessionId, string clientHash, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var invite = link?.Invite;
        var filingName = FormFilingNames.FilingName(form, answers, invite?.PersonName);
        var folder = await FormFolderFiling.FolderForAsync(context, form, filingName, now, cancellationToken);
        var submission = PublicFormSubmissions.New(form, answers, link, folder, sessionId, clientHash, now);
        context.FormSubmissions.Add(submission);
        foreach (var upload in uploads) upload.FormSubmissionId = submission.FormSubmissionId;
        if (link is not null) await SpendAsync(link, submission, now);
        context.WorkstationActions.AddRange(PublicFormSubmissions.WorkstationActionsFor(form, submission, answers, now));
        await context.SaveChangesAsync(cancellationToken);
        return submission;
    }

    private async Task SpendAsync(ResolvedLink link, FormSubmissionEntity submission, DateTimeOffset now)
    {
        var invite = link.Invite!;
        invite.UsedAt = now;
        invite.FormSubmissionId = submission.FormSubmissionId;
        if (link.Pack is not { } pack) return;
        var invites = await PackInvitesAsync(pack.FormPackId);
        FormPackLife.RecordFormSent(pack, invites, now);
    }
}
