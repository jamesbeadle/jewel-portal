using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>
/// Replaces the questionnaire wholesale: rows in, positions re-minted 1..n in the order they
/// arrived, blank rows (no question, no answer) dropped. The RequestItems arrangement — the editor
/// saves the whole sheet, so there is never a half-updated questionnaire.
/// </summary>
public sealed class SetTenderEnquiryAnswersHandler
    : ICommandHandler<SetTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>>
{
    private const int QuestionMaxChars = 2048;
    private const int AnswerMaxChars = 8000;

    private readonly JpmsContext context;

    public SetTenderEnquiryAnswersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TenderEnquiryAnswer>> HandleAsync(
        SetTenderEnquiryAnswers command, CancellationToken cancellationToken)
    {
        var enquiryExists = await context.TenderEnquiries
            .AnyAsync(row => row.TenderEnquiryId == command.TenderEnquiryId, cancellationToken);
        if (!enquiryExists) throw new InvalidOperationException($"Tender enquiry '{command.TenderEnquiryId}' not found.");

        var existing = await context.TenderEnquiryAnswers
            .Where(row => row.TenderEnquiryId == command.TenderEnquiryId)
            .ToListAsync(cancellationToken);
        context.TenderEnquiryAnswers.RemoveRange(existing);

        var position = 0;
        foreach (var draft in command.Answers.Where(HasContent))
        {
            position++;
            context.TenderEnquiryAnswers.Add(new TenderEnquiryAnswerEntity
            {
                TenderEnquiryAnswerId = TenderEnquiryIdentifierFactory.Next(),
                TenderEnquiryId = command.TenderEnquiryId,
                Position = position,
                Question = TenderEnquiryDetailsRules.Clamp(draft.Question, QuestionMaxChars),
                Answer = TenderEnquiryDetailsRules.Clamp(draft.Answer, AnswerMaxChars)
            });
        }
        await context.SaveChangesAsync(cancellationToken);
        return await TenderEnquiryAnswerReader.ListAsync(context, command.TenderEnquiryId, cancellationToken);
    }

    private static bool HasContent(TenderEnquiryAnswerDraft draft) =>
        !string.IsNullOrWhiteSpace(draft.Question) || !string.IsNullOrWhiteSpace(draft.Answer);
}
