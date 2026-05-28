using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class IssueDrawingRevisionValidation
{
    public ValidationOutcome Check(IssueDrawingRevision command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DrawingId)) errors.Add("DrawingId is required.");
        if (string.IsNullOrWhiteSpace(command.RevisionLabel)) errors.Add("Revision label is required.");
        if (string.IsNullOrWhiteSpace(command.FileName)) errors.Add("File name is required.");
        if (string.IsNullOrWhiteSpace(command.IssuedByEmail)) errors.Add("Issuing email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
