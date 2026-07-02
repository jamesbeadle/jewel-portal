using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class UpdateRequestFormValidation
{
    // Mirrors the entity's column widths so a too-long paste fails with a friendly message rather
    // than a database error.
    private const int SectionMax = 4000;
    private const int ImpactMax = 2048;
    private const int DrawingRefMax = 1024;
    private const int MemberAreaMax = 512;
    private const int QueryMax = 4000;

    public ValidationOutcome Check(UpdateRequestForm command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.RequestId)) errors.Add("RequestId is required.");
        if (command.BasisOfQueries is { Length: > SectionMax }) errors.Add($"Basis of queries must be {SectionMax} characters or fewer.");
        if (command.ResponseActionRequired is { Length: > SectionMax }) errors.Add($"Response / action required must be {SectionMax} characters or fewer.");
        if (command.ImpactIfLate is { Length: > ImpactMax }) errors.Add($"Impact if late must be {ImpactMax} characters or fewer.");

        var position = 0;
        foreach (var item in command.Items ?? (IReadOnlyList<RequestItemDraft>)Array.Empty<RequestItemDraft>())
        {
            position++;
            if (item.DrawingRef is { Length: > DrawingRefMax }) errors.Add($"Item {position}: drawing ref must be {DrawingRefMax} characters or fewer.");
            if (item.MemberArea is { Length: > MemberAreaMax }) errors.Add($"Item {position}: member / area must be {MemberAreaMax} characters or fewer.");
            if (item.Query is { Length: > QueryMax }) errors.Add($"Item {position}: query must be {QueryMax} characters or fewer.");
            if (item.Response is { Length: > QueryMax }) errors.Add($"Item {position}: response must be {QueryMax} characters or fewer.");
        }

        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
