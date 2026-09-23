using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>One form to one named person, as a link that is theirs alone (api/invite.js op:'send').</summary>
public sealed class SendFormInviteHandler : ICommandHandler<SendFormInvite, SentFormLink>
{
    private readonly JpmsContext context;
    private readonly IFormMailer mailer;
    private readonly FormSiteOptions options;

    public SendFormInviteHandler(JpmsContext context, IFormMailer mailer, FormSiteOptions options)
    {
        this.context = context;
        this.mailer = mailer;
        this.options = options;
    }

    public async Task<SentFormLink> HandleAsync(SendFormInvite command, CancellationToken cancellationToken)
    {
        var form = FormCatalogue.For(command.FormSlug) ?? throw new InvalidOperationException("Unknown form.");
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(FormLinkLifetimes.InviteDays(command.Days));
        var recipient = new FormLinkRecipient(command.Company, command.PersonName, command.CompanyName ?? "", command.Email);
        var sender = new FormLinkSender(command.SentByEmail, command.SentByName);
        var issued = FormInviteRows.New(form.Slug, recipient, sender, now, expiresAt, command.Reason ?? "", null);
        context.FormInvites.Add(issued.Invite);
        await context.SaveChangesAsync(cancellationToken);
        return await FormLinkMailing.SendOneAsync(mailer, options, form, issued, cancellationToken);
    }
}

public sealed class SendFormInviteAuthorisation
{
    public bool Allows(SignedInUser user, SendFormInvite command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class SendFormInviteValidation
{
    private const int LongestReason = 300;

    public ValidationOutcome Check(SendFormInvite command)
    {
        var errors = new List<string>();
        var reason = command.Reason ?? "";
        if (FormCatalogue.For(command.FormSlug) is null) errors.Add("Unknown form.");
        if (!Enum.IsDefined(command.Company)) errors.Add("Say which Jewel company the form is for.");
        if (string.IsNullOrWhiteSpace(command.PersonName)) errors.Add("Who is it for?");
        if (!FormInviteRows.IsAnEmailAddress(command.Email ?? "")) errors.Add("That email address does not look right.");
        if (reason.Length > LongestReason) errors.Add($"Keep the reason to {LongestReason} characters.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
