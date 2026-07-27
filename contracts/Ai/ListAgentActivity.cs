using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// The agent activity log, newest first. Optional filters; all null means everything.
/// </summary>
public sealed record ListAgentActivity(
    string? ProjectId = null,
    string? AgentKey = null,
    /// <summary>True to show only runs where no human was in the loop.</summary>
    bool? AutonomousOnly = null,
    int Take = 200) : IQuery<IReadOnlyList<AgentActivity>>;
