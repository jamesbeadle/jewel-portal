using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Registers.Policies;

/// <summary>
/// What a sent Policy sign-off form does: the policy and its declaration are stamped into the answers
/// from the invite's revision — never taken from the page — and the person's sign-off row is signed
/// with the typed signature, their company and position, the form, and the server's time, in the same
/// save as the form itself. A revision already signed keeps its first signature and the form stays on
/// record; a revision superseded since the link went cannot be signed at all.
/// </summary>
internal static class PolicySignOffSigning
{
    public static async Task<PolicyDocumentEntity?> PolicyOfAsync(
        JpmsContext context, FormInviteEntity? invite, CancellationToken cancellationToken)
    {
        var policyDocumentId = invite?.PolicyDocumentId;
        if (policyDocumentId is null) return null;
        return await context.PolicyDocuments.AsNoTracking()
            .FirstOrDefaultAsync(row => row.PolicyDocumentId == policyDocumentId, cancellationToken);
    }

    public static PublicPolicyFacts FactsOf(PolicyDocumentEntity policy) => new(
        PolicyDeclarations.PolicyLine(policy.Title, policy.Revision), PolicyDeclarations.Of(policy.Declaration));

    public static void StampAnswers(PolicyDocumentEntity policy, Dictionary<string, string> answers)
    {
        var facts = FactsOf(policy);
        answers[PolicySignOffForm.PolicyKey] = facts.PolicyLine;
        answers[PolicySignOffForm.DeclarationKey] = facts.Declaration;
    }

    public static async Task SignAsync(
        JpmsContext context, PolicyDocumentEntity policy, FormInviteEntity invite, string formSubmissionId,
        IReadOnlyDictionary<string, string> answers, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var email = invite.Email.Trim().ToLowerInvariant();
        var row = await PolicySignOffRequests.RowForAsync(context, policy.PolicyDocumentId, email, invite.FormInviteId, cancellationToken);
        if (row is null)
        {
            row = PolicySignOffRequests.NewRow(policy, invite.SentAt);
            row.RecipientEmail = email;
            context.PolicySignOffs.Add(row);
        }
        var isAlreadySigned = row.SignedAt is not null;
        if (isAlreadySigned) return;
        Sign(row, invite, formSubmissionId, answers, now);
    }

    private static void Sign(
        PolicySignOffEntity row, FormInviteEntity invite, string formSubmissionId, IReadOnlyDictionary<string, string> answers, DateTimeOffset now)
    {
        string Answer(string key) => answers.GetValueOrDefault(key, "").Trim();
        row.SignedAt = now;
        row.SignedName = Answer(FormAnswerRules.SignatureNameKey(PolicySignOffForm.SignatureKey));
        row.RecipientName = Answer(PolicySignOffForm.NameKey);
        row.CompanyName = Answer(PolicySignOffForm.CompanyKey);
        row.Position = Answer(PolicySignOffForm.PositionKey);
        row.FormInviteId = invite.FormInviteId;
        row.FormSubmissionId = formSubmissionId;
    }
}

/// <summary>The two fixed answers a policy sign-off carries: which revision, and the declaration signed to.</summary>
internal sealed record PublicPolicyFacts(string PolicyLine, string Declaration);
