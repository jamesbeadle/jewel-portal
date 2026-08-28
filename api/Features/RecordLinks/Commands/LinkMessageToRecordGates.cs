using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

// Gate classes for LinkMessageToRecord (2026-08-28), so the connector's file_email_to_record
// action can execute through the SAME checks as the HTTP route. RecordLinksEndpoints.Gate keeps
// its own check for the whole endpoint family — both read the one TriageRoles.AllowedToTriage
// RoleSet, so the two cannot drift.

public sealed class LinkMessageToRecordAuthorisation
{
    public bool Allows(SignedInUser user, LinkMessageToRecord command) =>
        TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}

public sealed class LinkMessageToRecordValidation
{
    // The HTTP route's only up-front check is a readable body; the ids are what the handler
    // cannot help with, so refuse their absence cleanly here.
    public ValidationOutcome Check(LinkMessageToRecord command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("messageId is required — read_record_emails and read_selected_email return it.");
        if (string.IsNullOrWhiteSpace(command.RecordId)) errors.Add("recordId is required — find_by_reference resolves a reference to it.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
