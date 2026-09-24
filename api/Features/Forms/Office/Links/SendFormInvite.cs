using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Api.Features.Registers.Policies;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>
/// One form to one named person, as a link that is theirs alone (api/invite.js op:'send'). A policy
/// sign-off goes for the current revision of the policy its sender chose, and asks for that
/// signature on the Policies page in the same save.
/// </summary>
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
        var recipient = new FormLinkRecipient(command.PersonName, command.CompanyName ?? "", command.Email);
        var sender = new FormLinkSender(command.SentByEmail, command.SentByName);
        var policy = form.IsSentByLinkOnly
            ? await PolicySignOffRequests.SignablePolicyAsync(context, command.PolicyDocumentId, cancellationToken)
            : null;
        var reason = ReasonFor(command, policy);
        var issued = FormInviteRows.New(form.Slug, recipient, sender, now, expiresAt, reason, null);
        issued.Invite.PolicyDocumentId = policy?.PolicyDocumentId;
        context.FormInvites.Add(issued.Invite);
        if (policy is not null) await PolicySignOffRequests.AskAsync(context, policy, issued.Invite, null, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await FormLinkMailing.SendOneAsync(mailer, options, form, issued, cancellationToken);
    }

    private static string ReasonFor(SendFormInvite command, PolicyDocumentEntity? policy)
    {
        var typed = command.Reason ?? "";
        var isLeftBlank = typed.Trim().Length == 0;
        return isLeftBlank && policy is not null ? PolicySignOffRequests.ReasonFor(policy) : typed;
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
        var form = FormCatalogue.For(command.FormSlug);
        if (form is null) errors.Add("Unknown form.");
        var needsAPolicy = form?.IsSentByLinkOnly == true && string.IsNullOrWhiteSpace(command.PolicyDocumentId);
        if (needsAPolicy) errors.Add("Choose the policy they are to sign.");
        if (string.IsNullOrWhiteSpace(command.PersonName)) errors.Add("Who is it for?");
        if (!FormInviteRows.IsAnEmailAddress(command.Email ?? "")) errors.Add("That email address does not look right.");
        if (reason.Length > LongestReason) errors.Add($"Keep the reason to {LongestReason} characters.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
