using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class SetTenderEnquiryAnswersValidation
{
    private const int MaxQuestions = 200;

    public ValidationOutcome Check(SetTenderEnquiryAnswers command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TenderEnquiryId)) errors.Add("TenderEnquiryId is required.");
        if (command.Answers is null) errors.Add("Answers are required (an empty list clears the sheet).");
        if (command.Answers is { Count: > MaxQuestions }) errors.Add($"A questionnaire is limited to {MaxQuestions} questions.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
