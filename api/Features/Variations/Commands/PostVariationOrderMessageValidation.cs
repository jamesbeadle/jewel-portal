using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class PostVariationOrderMessageValidation
{
    private const int BodyLimit = 4000;

    public ValidationOutcome Check(PostVariationOrderMessage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.VariationOrderId)) errors.Add("VariationOrderId is required.");
        if (string.IsNullOrWhiteSpace(command.Body)) errors.Add("Message body is required.");
        if (command.Body is { Length: > BodyLimit }) errors.Add($"Message body must be {BodyLimit} characters or fewer.");
        if (string.IsNullOrWhiteSpace(command.AuthorEmail)) errors.Add("Author email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
