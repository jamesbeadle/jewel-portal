using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Api.Features.Registers.Policies;

/// <summary>
/// Chasing one outstanding signature: when a Policy sign-off link is out, it is sent again — a fresh
/// link, the old one dead — exactly as Resend on the Sent out screen does; when the person was asked
/// on their portal login, they get their first link. Either way the current revision, never an old one.
/// </summary>
public sealed class ChasePolicySignOffHandler : ICommandHandler<ChasePolicySignOff, SentFormLink>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<ResendFormInvite, SentFormLink> resend;
    private readonly ICommandHandler<SendFormInvite, SentFormLink> send;

    public ChasePolicySignOffHandler(
        JpmsContext context, ICommandHandler<ResendFormInvite, SentFormLink> resend, ICommandHandler<SendFormInvite, SentFormLink> send)
    {
        this.context = context;
        this.resend = resend;
        this.send = send;
    }

    public async Task<SentFormLink> HandleAsync(ChasePolicySignOff command, CancellationToken cancellationToken)
    {
        var row = await context.PolicySignOffs.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.PolicySignOffId == command.PolicySignOffId, cancellationToken)
            ?? throw new InvalidOperationException("That sign-off no longer exists.");
        if (row.SignedAt is not null) throw new InvalidOperationException($"{row.RecipientEmail} has already signed — there is nothing to chase.");
        await PolicySignOffRequests.SignablePolicyAsync(context, row.PolicyDocumentId, cancellationToken);
        var liveInviteId = await LiveInviteIdAsync(row.FormInviteId, cancellationToken);
        if (liveInviteId is not null)
            return await resend.HandleAsync(new ResendFormInvite(liveInviteId, "", command.SentByEmail, command.SentByName), cancellationToken);
        var firstLink = new SendFormInvite(
            FormSlugs.PolicySignOff, NameOf(row.RecipientName, row.RecipientEmail), row.RecipientEmail, row.CompanyName,
            FormLinkLifetimes.DefaultInviteDays, "", command.SentByEmail, command.SentByName, row.PolicyDocumentId);
        return await send.HandleAsync(firstLink, cancellationToken);
    }

    private async Task<string?> LiveInviteIdAsync(string? formInviteId, CancellationToken cancellationToken)
    {
        if (formInviteId is null) return null;
        var invite = await context.FormInvites.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.FormInviteId == formInviteId, cancellationToken);
        var canBeSentAgain = invite is { UsedAt: null };
        return canBeSentAgain ? invite!.FormInviteId : null;
    }

    private static string NameOf(string recipientName, string email) => recipientName.Trim().Length > 0 ? recipientName : email;
}

public sealed class ChasePolicySignOffAuthorisation
{
    public bool Allows(SignedInUser user, ChasePolicySignOff command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class ChasePolicySignOffValidation
{
    public ValidationOutcome Check(ChasePolicySignOff command) =>
        string.IsNullOrWhiteSpace(command.PolicySignOffId) ? new ValidationOutcome(new[] { "Which signature?" }) : ValidationOutcome.Passed;
}
