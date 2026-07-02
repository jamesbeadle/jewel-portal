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
        // No kind requires a reference at creation: a blank one is minted server-side — General
        // requests are numbered REQ-#### (global sequence), any other kind (e.g. an RFI) continues
        // the project's own sequence. A typed reference (legacy back-fill) is honoured as given.
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(command.RaisedByEmail)) errors.Add("Raised-by email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
