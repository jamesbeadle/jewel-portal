using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One skill attached to one connector action — or to a whole action area (2026-08-31). The
/// actions themselves are code (AiActionRegistry); the skills are rows (SkillEntity); this row is
/// the edge between them, so the wiring is data the discipline owner curates on the AI Actions
/// admin page, never a deploy. describe_action resolves a target's attachments live on every call
/// and inlines the attached skills' bodies next to the argument schema.
/// </summary>
public sealed class AiActionSkillEntity
{
    [Key, MaxLength(64)] public string ActionSkillId { get; set; } = "";

    /// <summary>AiActionSkillTargets.Action or AiActionSkillTargets.Area — whether TargetKey names
    /// one action or every action in an area. Loose string on purpose, like every link in this
    /// schema.</summary>
    [MaxLength(16)] public string TargetKind { get; set; } = "";

    /// <summary>The action name ("approve_variation_order") or the area exactly as the registry
    /// declares it ("Variations"). Not a foreign key: the registry is code, and an attachment to a
    /// renamed action simply stops matching — the admin page shows such orphans for tidying.</summary>
    [MaxLength(128)] public string TargetKey { get; set; } = "";

    [MaxLength(128)] public string SkillKey { get; set; } = "";

    [MaxLength(256)] public string AttachedByEmail { get; set; } = "";
    public DateTimeOffset AttachedAt { get; set; }
}
