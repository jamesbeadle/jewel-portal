using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Changes;

namespace Jewel.JPMS.Api.Features.Changes.Commands;

public sealed class RaiseChangeValidation
{
    public ValidationOutcome Check(RaiseChange command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Reference)) errors.Add("Reference is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(command.RaisedByEmail)) errors.Add("Raised-by email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
