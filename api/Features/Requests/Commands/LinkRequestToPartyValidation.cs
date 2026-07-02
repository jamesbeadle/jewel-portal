using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class LinkRequestToPartyValidation
{
    public ValidationOutcome Check(LinkRequestToParty command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.RequestId))
            errors.Add("RequestId is required.");
        if (command.PartyKind is not (PartyKind.Client or PartyKind.Architect))
            errors.Add("PartyKind must be Client or Architect.");
        if (!string.IsNullOrWhiteSpace(command.OnBehalfOfClientId) && command.PartyKind != PartyKind.Architect)
            errors.Add("OnBehalfOfClientId only applies when the party is an architect.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
