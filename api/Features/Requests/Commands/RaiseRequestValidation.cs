using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class RaiseRequestValidation
{
    public ValidationOutcome Check(RaiseRequest command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        // A General request is a container auto-numbered REQ-#### server-side, so no reference is
        // required at creation. Any other kind (e.g. a back-filled RFI) must still carry one.
        if (command.Kind != RequestType.General && string.IsNullOrWhiteSpace(command.Reference)) errors.Add("Reference is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(command.RaisedByEmail)) errors.Add("Raised-by email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
