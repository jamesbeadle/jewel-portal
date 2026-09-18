using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests.Recipients;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed partial class RequestEmailComposer
{
    /// <summary>An ad-hoc override addresses the draft to that one email, nothing copied;
    /// otherwise the shared resolver supplies the full To/CC/BCC set (request party → project
    /// party → project profile To rows, with the correspondence profile supplying CC/BCC).</summary>
    private async Task<RequestRecipientSet> RecipientsForAsync(
        RequestEntity request, string? recipientOverride, CancellationToken cancellationToken)
    {
        var recipients = string.IsNullOrWhiteSpace(recipientOverride)
            ? await RequestRecipientResolver.ResolveAsync(context, request, cancellationToken)
            : AddressedTo(recipientOverride);
        if (recipients.HasTo) return recipients;

        throw new InvalidOperationException(
            "No recipient could be resolved. Link the request (or its project) to a client or architect " +
            "with a contact, or set a project contact's routing to To.");
    }

    private static RequestRecipientSet AddressedTo(string recipientOverride) =>
        new(new[] { new CorrespondenceRecipient("", recipientOverride.Trim(), CorrespondenceRouting.To) },
            Array.Empty<CorrespondenceRecipient>(),
            Array.Empty<CorrespondenceRecipient>());

    /// <summary>The resolved Cc plus the projects mailbox, which the Graph client copies on every
    /// message itself — the confirmation the user reads must match the email that actually went.</summary>
    private List<string> CopiedRecipients(RequestRecipientSet recipients)
    {
        var copied = recipients.Cc.Select(recipient => recipient.Email).ToList();
        if (string.IsNullOrWhiteSpace(mailboxOptions.Mailbox)) return copied;

        var alreadyThere = recipients.To.Concat(recipients.Cc).Concat(recipients.Bcc)
            .Any(recipient => string.Equals(recipient.Email.Trim(), mailboxOptions.Mailbox, StringComparison.OrdinalIgnoreCase));
        if (alreadyThere) return copied;

        copied.Add(mailboxOptions.Mailbox.Trim());
        return copied;
    }

    private static MailboxDraftRecipient ToDraftRecipient(CorrespondenceRecipient recipient) =>
        new(recipient.Email, string.IsNullOrWhiteSpace(recipient.Name) ? null : recipient.Name);
}
