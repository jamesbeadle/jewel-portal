using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Answers;
using Jewel.JPMS.Api.Features.Forms.Filing;
using Jewel.JPMS.Api.Features.Forms.Links;

namespace Jewel.JPMS.Api.Features.Forms.Public;

/// <summary>
/// The rows a sent form writes: the submission itself — who it is from, what it is filed under,
/// whether a one-time link vouches for it — and, for a workstation assessment, one action for every
/// NO, because the noes across every assessment are the office's work queue.
/// </summary>
internal static class PublicFormSubmissions
{
    private const string WorkstationKey = "where";

    public static FormSubmissionEntity New(
        FormDefinition form, JewelCompany company, IReadOnlyDictionary<string, string> answers, ResolvedLink? link,
        FormFolderEntity folder, string sessionId, string clientHash, DateTimeOffset now)
    {
        var invite = link?.Invite;
        return new FormSubmissionEntity
        {
            FormSubmissionId = FormIdentifierFactory.NextId(),
            FormSlug = form.Slug,
            Company = (int)company,
            SessionId = sessionId,
            FormInviteId = invite?.FormInviteId,
            FormPackId = invite?.FormPackId,
            FormFolderId = folder.FormFolderId,
            SubmitterName = FormFilingNames.SubmitterName(form, answers, invite?.PersonName),
            FilingName = folder.Name,
            IsVerifiedLink = invite is not null,
            SentToEmail = invite?.Email ?? "",
            SentByName = invite?.SentByName ?? "",
            AnswersJson = FormAnswersJson.Write(answers),
            Status = (int)FormSubmissionStatus.New,
            SubmittedAt = now,
            ClientHash = clientHash
        };
    }

    public static IEnumerable<WorkstationActionEntity> WorkstationActionsFor(
        FormDefinition form, FormSubmissionEntity submission, IReadOnlyDictionary<string, string> answers, DateTimeOffset now)
    {
        var isAnAssessment = form.Slug == FormSlugs.WorkstationAssessment;
        if (!isAnAssessment) return Array.Empty<WorkstationActionEntity>();
        var workstation = answers.GetValueOrDefault(WorkstationKey, "");
        return WorkstationActionPlanner.ActionsFor(answers).Select(needed => new WorkstationActionEntity
        {
            WorkstationActionId = FormIdentifierFactory.NextId(),
            FormSubmissionId = submission.FormSubmissionId,
            PersonName = submission.SubmitterName,
            Workstation = workstation,
            QuestionKey = needed.QuestionKey,
            Action = needed.Action,
            State = (int)WorkstationActionState.Open,
            RaisedAt = now
        }).ToList();
    }
}
