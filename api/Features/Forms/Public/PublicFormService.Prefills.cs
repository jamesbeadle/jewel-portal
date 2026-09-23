using Jewel.JPMS.Api.Features.Forms.Answers;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

public sealed partial class PublicFormService
{
    /// <summary>
    /// Who a linked form is for, and what it opens with: the invite's name, company and email, and —
    /// across a pack — what the person already typed on the pack's earlier forms, so they type their
    /// details once. Only a general-store form's answers travel, and never one its form treats as
    /// sensitive: a right-to-work or starter-checklist answer stays in its restricted store.
    /// </summary>
    private async Task<PublicFormInvitation> InvitationForAsync(
        FormDefinition form, JewelCompany company, ResolvedLink link, CancellationToken cancellationToken)
    {
        var invite = link.Invite!;
        var carried = link.Pack is null
            ? new Dictionary<string, string>()
            : await CarriedAnswersAsync(link.Pack.FormPackId, cancellationToken);
        var prefills = FormPrefills.For(form, company, invite.PersonName, invite.CompanyName, invite.Email, carried);
        return new PublicFormInvitation(invite.PersonName, invite.CompanyName, invite.Email, invite.SentByName, prefills);
    }

    private async Task<Dictionary<string, string>> CarriedAnswersAsync(string formPackId, CancellationToken cancellationToken)
    {
        var sent = await context.FormSubmissions.AsNoTracking()
            .Where(row => row.FormPackId == formPackId && row.DestroyedAt == null)
            .OrderBy(row => row.SubmittedAt)
            .Select(row => new { row.FormSlug, row.AnswersJson })
            .ToListAsync(cancellationToken);
        var carried = new Dictionary<string, string>();
        foreach (var earlier in sent) CarryFrom(FormCatalogue.For(earlier.FormSlug), FormAnswersJson.Read(earlier.AnswersJson), carried);
        return carried;
    }

    private static void CarryFrom(FormDefinition? form, IReadOnlyDictionary<string, string> answers, Dictionary<string, string> carried)
    {
        if (form is not { Store: FormEvidenceStore.General }) return;
        var shareable = answers.Where(pair => form.QuestionFor(pair.Key) is { } question && !SensitiveAnswers.IsSensitive(question));
        foreach (var (key, answer) in shareable) carried[key] = answer;
    }
}
