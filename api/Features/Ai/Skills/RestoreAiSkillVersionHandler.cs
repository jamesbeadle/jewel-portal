using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>
/// A restore is an ordinary save of the earlier version's words, through the same save handlers
/// the page and the connector use — so the version it replaces is kept like any other, and a
/// restore can be undone by restoring again. Nothing in the history is ever removed.
/// </summary>
public sealed class RestoreAiSkillVersionHandler : ICommandHandler<RestoreAiSkillVersion, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IQueryHandler<GetAiSkillHistory, SkillHistory?> histories;
    private readonly ICommandHandler<SaveAiSkill, Acknowledgement> saveSkill;
    private readonly ICommandHandler<SaveAiSkillReference, Acknowledgement> saveReference;

    public RestoreAiSkillVersionHandler(
        JpmsContext context,
        IQueryHandler<GetAiSkillHistory, SkillHistory?> histories,
        ICommandHandler<SaveAiSkill, Acknowledgement> saveSkill,
        ICommandHandler<SaveAiSkillReference, Acknowledgement> saveReference)
    {
        this.context = context;
        this.histories = histories;
        this.saveSkill = saveSkill;
        this.saveReference = saveReference;
    }

    public async Task<Acknowledgement> HandleAsync(RestoreAiSkillVersion command, CancellationToken cancellationToken)
    {
        var skillKey = command.SkillKey.Trim();
        var history = await histories.HandleAsync(new GetAiSkillHistory(skillKey), cancellationToken)
            ?? throw new InvalidOperationException($"No skill named {skillKey} exists.");

        var refKey = command.RefKey?.Trim();
        if (string.IsNullOrEmpty(refKey)) return await RestoreSkillAsync(history, command, cancellationToken);

        var reference = history.References.FirstOrDefault(candidate => candidate.RefKey == refKey)
            ?? throw new InvalidOperationException($"The skill {skillKey} has no reference named {refKey}.");
        var version = VersionToRestore(reference.Versions, command.Version, $"{skillKey}/{refKey}");
        return await saveReference.HandleAsync(new SaveAiSkillReference(
            skillKey, refKey, NameOf(version, reference.DisplayName), version.Description, version.Body,
            command.RestoredByEmail), cancellationToken);
    }

    private async Task<Acknowledgement> RestoreSkillAsync(
        SkillHistory history, RestoreAiSkillVersion command, CancellationToken cancellationToken)
    {
        var version = VersionToRestore(history.Versions, command.Version, history.SkillKey);
        var current = await context.Skills.AsNoTracking()
            .FirstAsync(row => row.SkillKey == history.SkillKey, cancellationToken);
        return await saveSkill.HandleAsync(new SaveAiSkill(
            history.SkillKey, current.AgentKey, NameOf(version, current.DisplayName), version.Description,
            version.Body, current.Pinned, current.IsActive, command.RestoredByEmail), cancellationToken);
    }

    private static SkillVersion VersionToRestore(IReadOnlyList<SkillVersion> versions, int number, string document)
    {
        var version = SkillVersionTimeline.Numbered(versions, number)
            ?? throw new InvalidOperationException($"{document} has no version {number}.");
        if (version.IsCurrent)
            throw new InvalidOperationException($"Version {number} of {document} is already the one in force.");
        return version;
    }

    private static string NameOf(SkillVersion version, string currentName) =>
        string.IsNullOrWhiteSpace(version.DisplayName) ? currentName : version.DisplayName;
}
