using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// Attaching skills to the connector's actions (2026-08-31). An attachment says "when the model is
/// about to perform THIS action (or any action in THIS area), this skill's doctrine belongs in its
/// context" — describe_action inlines the attached skills' bodies alongside the argument schema,
/// so the guidance rides in on the road the model must travel to perform anything. Actions are
/// code (AiActionRegistry); skills are database rows; the attachment is the database's edge
/// between them, curated on the AI Actions admin page by the same people who own the skills.
/// </summary>
public static class AiActionSkillTargets
{
    public const string Action = "action";
    public const string Area = "area";
}

/// <summary>One action as the admin page lists it — the registry entry's identity, not its schema.</summary>
public sealed record AiActionSummary(
    string Name,
    string Area,
    string Summary,
    bool RequiresConfirmation);

/// <summary>One attachment edge: a skill wired to one action, or to every action in an area.</summary>
public sealed record AiActionSkillAttachment(
    string TargetKind,
    string TargetKey,
    string SkillKey,
    string AttachedByEmail,
    DateTimeOffset AttachedAt);

/// <summary>Everything the AI Actions admin page shows in one fetch, so the panel reveals in one
/// piece: the whole action registry and every attachment. Skills come from ListAiSkills.</summary>
public sealed record AiActionCatalogue(
    IReadOnlyList<AiActionSummary> Actions,
    IReadOnlyList<AiActionSkillAttachment> Attachments);

public sealed record GetAiActionCatalogue : IQuery<AiActionCatalogue>;

/// <summary>
/// Replace one target's attached-skill set in a single write — the admin page's checkbox picker
/// saved whole, so detaching is the same act as attaching. <c>SavedByEmail</c> is re-stamped from
/// the session.
/// </summary>
public sealed record SaveAiActionSkills(
    string TargetKind,
    string TargetKey,
    IReadOnlyList<string> SkillKeys,
    string SavedByEmail) : ICommand<Acknowledgement>;
