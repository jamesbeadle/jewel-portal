using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PrepareRequestEmailDraftsValidation
{
    // Each draft renders a PDF and makes a Graph call, so a run is capped to keep one HTTP
    // invocation comfortably inside function and client timeouts. The UI chunks larger
    // selections into successive calls.
    public const int MaxPerCall = 10;

    public ValidationOutcome Check(PrepareRequestEmailDrafts command)
    {
        var errors = new List<string>();
        if (command.RequestIds is not { Count: > 0 })
            errors.Add("At least one RequestId is required.");
        else
        {
            if (command.RequestIds.Any(string.IsNullOrWhiteSpace))
                errors.Add("RequestIds must not contain blank entries.");
            if (command.RequestIds.Count > MaxPerCall)
                errors.Add($"At most {MaxPerCall} requests can be drafted per call.");
        }
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
