using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Registers.Policies;

/// <summary>
/// Asking someone to sign a policy by form link: the revision must be the current one, and the
/// person's sign-off row — one per revision and email — is found or made and pointed at the live
/// link, so the Policies page shows them as outstanding from the moment the link goes. Somebody who
/// has already signed that revision is not asked again.
/// </summary>
internal static class PolicySignOffRequests
{
    public static async Task<PolicyDocumentEntity> SignablePolicyAsync(
        JpmsContext context, string? policyDocumentId, CancellationToken cancellationToken)
    {
        var policy = string.IsNullOrWhiteSpace(policyDocumentId)
            ? null
            : await context.PolicyDocuments.FirstOrDefaultAsync(row => row.PolicyDocumentId == policyDocumentId, cancellationToken);
        if (policy is null) throw new InvalidOperationException("Choose a published policy for them to sign.");
        if (!policy.IsActive)
            throw new InvalidOperationException($"Revision {policy.Revision} of {policy.Title} has been superseded — send the current revision.");
        return policy;
    }

    public static string ReasonFor(PolicyDocumentEntity policy) =>
        $"Please read and sign our {PolicyDeclarations.PolicyLine(policy.Title, policy.Revision)}. "
        + "Everyone working with us signs the current revision; it only takes a few minutes and you can do it on your phone.";

    public static async Task AskAsync(
        JpmsContext context, PolicyDocumentEntity policy, FormInviteEntity invite, string? replacedInviteId, CancellationToken cancellationToken)
    {
        var email = invite.Email.Trim().ToLowerInvariant();
        var row = await RowForAsync(context, policy.PolicyDocumentId, email, replacedInviteId, cancellationToken);
        if (row?.SignedAt is { } signedAt)
            throw new InvalidOperationException($"{email} signed revision {policy.Revision} of {policy.Title} on {signedAt:d MMM yyyy} — there is nothing to ask.");
        if (row is null)
        {
            row = NewRow(policy, invite.SentAt);
            context.PolicySignOffs.Add(row);
        }
        row.RecipientEmail = email;
        row.RecipientName = invite.PersonName;
        row.FormInviteId = invite.FormInviteId;
    }

    public static async Task<PolicySignOffEntity?> RowForAsync(
        JpmsContext context, string policyDocumentId, string email, string? replacedInviteId, CancellationToken cancellationToken)
    {
        var sameEmail = await context.PolicySignOffs.FirstOrDefaultAsync(
            row => row.PolicyDocumentId == policyDocumentId && row.RecipientEmail == email, cancellationToken);
        if (sameEmail is not null || replacedInviteId is null) return sameEmail;
        return await context.PolicySignOffs.FirstOrDefaultAsync(
            row => row.FormInviteId == replacedInviteId && row.SignedAt == null, cancellationToken);
    }

    public static PolicySignOffEntity NewRow(PolicyDocumentEntity policy, DateTimeOffset requestedAt) => new()
    {
        PolicySignOffId = RegisterIdentifierFactory.NextPolicySignOffId(),
        PolicyDocumentId = policy.PolicyDocumentId,
        RequestedAt = requestedAt
    };
}
