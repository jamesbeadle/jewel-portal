using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>
/// A fresh link and a fresh expiry (api/invite.js op:'resend'). The old link dies with the new one,
/// so a forwarded email cannot be used later by whoever it was forwarded to. A form from a pack sent
/// again this way stays in its pack.
/// </summary>
public sealed class ResendFormInviteHandler : ICommandHandler<ResendFormInvite, SentFormLink>
{
    private readonly JpmsContext context;
    private readonly IFormMailer mailer;
    private readonly FormSiteOptions options;

    public ResendFormInviteHandler(JpmsContext context, IFormMailer mailer, FormSiteOptions options)
    {
        this.context = context;
        this.mailer = mailer;
        this.options = options;
    }

    public async Task<SentFormLink> HandleAsync(ResendFormInvite command, CancellationToken cancellationToken)
    {
        var old = await context.FormInvites.FirstOrDefaultAsync(row => row.FormInviteId == command.FormInviteId, cancellationToken)
            ?? throw new InvalidOperationException("That link no longer exists.");
        if (old.UsedAt is not null) throw new InvalidOperationException("That form has already been sent back — there is nothing to resend.");
        var form = FormCatalogue.For(old.FormSlug) ?? throw new InvalidOperationException("Unknown form.");
        var now = DateTimeOffset.UtcNow;
        old.CancelledAt ??= now;
        var email = string.IsNullOrWhiteSpace(command.Email) ? old.Email : command.Email.Trim();
        var recipient = new FormLinkRecipient(old.PersonName, old.CompanyName, email);
        var sender = new FormLinkSender(command.SentByEmail, command.SentByName);
        var expiresAt = now.AddDays(FormLinkLifetimes.DefaultInviteDays);
        var issued = FormInviteRows.New(old.FormSlug, recipient, sender, now, expiresAt, old.Reason, old.FormPackId);
        context.FormInvites.Add(issued.Invite);
        await context.SaveChangesAsync(cancellationToken);
        return await FormLinkMailing.SendOneAsync(mailer, options, form, issued, cancellationToken);
    }
}

public sealed class ResendFormInviteAuthorisation
{
    public bool Allows(SignedInUser user, ResendFormInvite command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class ResendFormInviteValidation
{
    public ValidationOutcome Check(ResendFormInvite command)
    {
        var errors = new List<string>();
        var email = command.Email ?? "";
        if (string.IsNullOrWhiteSpace(command.FormInviteId)) errors.Add("Which link?");
        var isANewAddress = email.Trim().Length > 0;
        if (isANewAddress && !FormInviteRows.IsAnEmailAddress(email)) errors.Add("That email address does not look right.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
