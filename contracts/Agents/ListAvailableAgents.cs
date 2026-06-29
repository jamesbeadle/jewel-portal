using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// Catalogue of the agents that can be applied to a request — drives the "apply agent" screen.
public sealed record ListAvailableAgents : IQuery<IReadOnlyList<AgentDescriptor>>;
