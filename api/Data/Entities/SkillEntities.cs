using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One skill: a versioned markdown manual attached to an agent (docs/ai/05-agents-and-skills.md).
/// This is the DOMAIN half of the agent split — the agent scaffolding is code
/// (contracts/Ai/AgentCatalogue.cs); the discipline knowledge is this row, edited in the portal by
/// the person who owns it. A new commercial rule is an update here, not a deploy.
/// </summary>
public sealed class SkillEntity
{
    /// <summary>agentskills.io name — "nigel-commercial-doctrine". Stable; edits version the row.</summary>
    [Key, MaxLength(128)] public string SkillKey { get; set; } = "";

    /// <summary>The AgentCatalogue key this skill belongs to, or "shared" — pinned for every
    /// agent (the JBB Second Brain). Loose string on purpose, like every link in this schema.</summary>
    [MaxLength(64)] public string AgentKey { get; set; } = "shared";

    [MaxLength(256)] public string DisplayName { get; set; } = "";

    /// <summary>The frontmatter description, verbatim. This is what the orchestrator routes on and
    /// what the agent reads when deciding to load an unpinned skill — write it for the model.</summary>
    [MaxLength(4000)] public string Description { get; set; } = "";

    /// <summary>The markdown body. Unbounded on purpose — a silently truncated doctrine is a model
    /// following half a method without knowing it.</summary>
    public string Body { get; set; } = "";

    /// <summary>Pinned rides in the system prompt whenever the owning agent is in force; unpinned
    /// is listed by name and pulled on demand with load_skill.</summary>
    public bool Pinned { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Incremented on every save; the outgoing body is copied to SkillRevisions first.
    /// The activity log records the versions in force per hop, so "what did the assistant know
    /// when it drafted this" stays answerable.</summary>
    public int Version { get; set; } = 1;

    [MaxLength(256)] public string UpdatedByEmail { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>A reference document under a skill — the agentskills.io references/ folder. Never
/// pinned: the model asks for one by key with load_skill_reference when it needs it.</summary>
public sealed class SkillReferenceEntity
{
    [Key, MaxLength(64)] public string SkillReferenceId { get; set; } = "";
    [MaxLength(128)] public string SkillKey { get; set; } = "";
    /// <summary>The file-ish key the model asks for — "jct-clause-map".</summary>
    [MaxLength(128)] public string RefKey { get; set; } = "";
    [MaxLength(256)] public string DisplayName { get; set; } = "";
    /// <summary>One or two clauses telling the model when this reference is worth loading.</summary>
    [MaxLength(2000)] public string Description { get; set; } = "";
    public string Body { get; set; } = "";
    [MaxLength(256)] public string UpdatedByEmail { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>The body a save replaced — kept so a doctrine edit is never destructive and an old
/// conversation's "versions in force" can be read back. Append-only.</summary>
public sealed class SkillRevisionEntity
{
    [Key, MaxLength(64)] public string SkillRevisionId { get; set; } = "";
    [MaxLength(128)] public string SkillKey { get; set; } = "";
    /// <summary>The version this body WAS — the row is written at the moment it is superseded.</summary>
    public int Version { get; set; }
    public string Body { get; set; } = "";
    [MaxLength(4000)] public string Description { get; set; } = "";
    [MaxLength(256)] public string SavedByEmail { get; set; } = "";
    public DateTimeOffset SavedAt { get; set; }
}
