using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// An agent applied to a request — one row in the global watch queue, per (request, agent).
// Enums (Status) are stored as int; no FK constraints, by-id only, matching every JPMS table.
public sealed class RequestAgentEntity
{
    [Key, MaxLength(64)] public string RequestAgentId { get; set; } = "";
    [MaxLength(64)]      public string RequestId { get; set; } = "";
    [MaxLength(64)]      public string AgentKey { get; set; } = "";
    public int Status { get; set; }
    public bool IsPrimary { get; set; }
    [MaxLength(1024)]    public string StatusMessage { get; set; } = "";
    [MaxLength(256)]     public string AssignedByEmail { get; set; } = "";
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

// A single message in a per-(request, agent) conversation. Role is stored as int.
public sealed class AgentChatMessageEntity
{
    [Key, MaxLength(64)] public string MessageId { get; set; } = "";
    [MaxLength(64)]      public string RequestId { get; set; } = "";
    [MaxLength(64)]      public string AgentKey { get; set; } = "";
    public int Role { get; set; }
    [MaxLength(256)]     public string AuthorEmail { get; set; } = "";
    [MaxLength(256)]     public string AuthorName { get; set; } = "";
    [MaxLength(4000)]    public string Body { get; set; } = "";
    public DateTimeOffset PostedAt { get; set; }
}

// A persisted structured proposal produced by an agent's analysis. StructuredJson is the raw
// discipline-specific object (nvarchar(max)); Status is stored as int.
public sealed class AgentProposalEntity
{
    [Key, MaxLength(64)] public string ProposalId { get; set; } = "";
    [MaxLength(64)]      public string RequestId { get; set; } = "";
    [MaxLength(64)]      public string AgentKey { get; set; } = "";
    public int Status { get; set; }
    [MaxLength(1024)]    public string Summary { get; set; } = "";
    public string StructuredJson { get; set; } = "";
    [MaxLength(4000)]    public string? Rationale { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)]     public string? DecidedByEmail { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}
