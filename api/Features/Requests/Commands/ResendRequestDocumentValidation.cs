using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class ResendRequestDocumentValidation
{
    public ValidationOutcome Check(ResendRequestDocument command)
    {
        if (string.IsNullOrWhiteSpace(command.RequestId))
            return ValidationOutcome.Failed("RequestId is required.");

        // The override is optional, but if supplied it must look like a single email address.
        if (!string.IsNullOrWhiteSpace(command.RecipientOverride))
        {
            var candidate = command.RecipientOverride.Trim();
            if (!candidate.Contains('@') || candidate.Contains(' ') || candidate.Contains(','))
                return ValidationOutcome.Failed("RecipientOverride must be a single valid email address.");
        }

        return ValidationOutcome.Passed;
    }
}
