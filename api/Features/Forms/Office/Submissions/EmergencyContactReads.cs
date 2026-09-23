using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Forms.Answers;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// Each person's latest emergency contact, laid out for whoever is on site when something happens —
/// the site manager and the H&amp;S lead as well as the office, because the point of collecting it is
/// to reach it fast. A form that came through the person's own link outranks any sent from the open
/// address under their name, and the card says when it did not. The health answers are not on the
/// card: only that there are some.
/// </summary>
public sealed class ListEmergencyContactsHandler : IQueryHandler<ListEmergencyContacts, IReadOnlyList<EmergencyContactCard>>
{
    private readonly JpmsContext context;

    public ListEmergencyContactsHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<EmergencyContactCard>> HandleAsync(ListEmergencyContacts query, CancellationToken cancellationToken)
    {
        var forms = await context.FormSubmissions.AsNoTracking()
            .Where(row => row.FormSlug == FormSlugs.EmergencyContact && row.DestroyedAt == null)
            .OrderByDescending(row => row.IsVerifiedLink)
            .ThenByDescending(row => row.SubmittedAt)
            .ToListAsync(cancellationToken);
        return forms.GroupBy(row => row.FormFolderId ?? row.FormSubmissionId)
            .Select(person => CardFor(person.First()))
            .OrderBy(card => card.PersonName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static EmergencyContactCard CardFor(FormSubmissionEntity submission)
    {
        var answers = FormAnswersJson.Read(submission.AnswersJson);
        var hasHealthAnswers = FormSubmissionReading.HealthAnswersOf(submission).Count > 0;
        return new EmergencyContactCard(
            submission.FormSubmissionId, submission.SubmitterName,
            answers.GetValueOrDefault("ec_name", ""), answers.GetValueOrDefault("relationship", ""),
            answers.GetValueOrDefault("ec_phone", ""), answers.GetValueOrDefault("ec_email", ""),
            answers.GetValueOrDefault("ec_address", ""), submission.SubmittedAt, hasHealthAnswers, submission.IsVerifiedLink);
    }
}

/// <summary>
/// The one read that answers an emergency form's health answers. The look is recorded before the
/// answers are given — not best-effort, as the rest of the audit trail is: a look that cannot be
/// recorded is not given. The row names the form and who looked, never the person or the answer.
/// </summary>
public sealed class RevealHealthAnswersHandler : IQueryHandler<RevealHealthAnswers, IReadOnlyDictionary<string, string>>
{
    private readonly JpmsContext context;
    private readonly AuditActor actor;

    public RevealHealthAnswersHandler(JpmsContext context, AuditActor actor)
    {
        this.context = context;
        this.actor = actor;
    }

    public async Task<IReadOnlyDictionary<string, string>> HandleAsync(RevealHealthAnswers query, CancellationToken cancellationToken)
    {
        var submission = await context.FormSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(row => row.FormSubmissionId == query.FormSubmissionId, cancellationToken)
            ?? throw new InvalidOperationException("That form no longer exists.");
        var detail = $"Health answers revealed on a {FormCatalogue.TitleOf(submission.FormSlug)}";
        context.AuditEvents.Add(FormAuditRecords.Of(AuditEventType.FormHealthAnswersRevealed, submission.FormSubmissionId, actor.Email, detail));
        await context.SaveChangesAsync(cancellationToken);
        return FormSubmissionReading.HealthAnswersOf(submission);
    }
}
