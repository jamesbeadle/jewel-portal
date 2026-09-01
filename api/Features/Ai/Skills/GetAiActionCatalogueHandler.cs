using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>
/// The whole action registry plus every skill attachment, in one answer — the AI Actions admin
/// page's single fetch. The registry is code, so the actions come from memory; the attachments are
/// rows, read fresh so another director's save is visible on the next load.
/// </summary>
public sealed class GetAiActionCatalogueHandler : IQueryHandler<GetAiActionCatalogue, AiActionCatalogue>
{
    private readonly JpmsContext context;

    public GetAiActionCatalogueHandler(JpmsContext context) => this.context = context;

    public async Task<AiActionCatalogue> HandleAsync(GetAiActionCatalogue query, CancellationToken cancellationToken)
    {
        var actions = AiActionRegistry.All
            .Select(action => new AiActionSummary(
                action.Name,
                action.Area,
                FirstSentence(action.Description),
                action.RequiresConfirmation))
            .ToList();

        var attachments = await context.AiActionSkills
            .AsNoTracking()
            .OrderBy(row => row.TargetKind).ThenBy(row => row.TargetKey).ThenBy(row => row.SkillKey)
            .Select(row => new AiActionSkillAttachment(
                row.TargetKind, row.TargetKey, row.SkillKey, row.AttachedByEmail, row.AttachedAt))
            .ToListAsync(cancellationToken);

        return new AiActionCatalogue(actions, attachments);
    }

    private static string FirstSentence(string description)
    {
        var stop = description.IndexOf(". ", StringComparison.Ordinal);
        return stop > 0 ? description[..(stop + 1)] : description;
    }
}
