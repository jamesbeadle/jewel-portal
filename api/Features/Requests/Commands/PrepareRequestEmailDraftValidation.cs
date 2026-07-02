using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PrepareRequestEmailDraftValidation
{
    public ValidationOutcome Check(PrepareRequestEmailDraft command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.RequestId)) errors.Add("RequestId is required.");
        if (command.RecipientOverride is { } email && !string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            errors.Add("RecipientOverride must be an email address.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
