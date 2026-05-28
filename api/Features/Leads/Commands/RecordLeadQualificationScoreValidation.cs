using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordLeadQualificationScoreValidation
{
    private const int MinimumScore = 0;
    private const int MaximumScore = 100;

    public ValidationOutcome Check(RecordLeadQualificationScore command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.AssessedByEmail)) errors.Add("Assessor email is required.");
        if (command.Score < MinimumScore || command.Score > MaximumScore)
            errors.Add($"Score must be between {MinimumScore} and {MaximumScore}.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
