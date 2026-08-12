using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The agent registry (docs/ai/05-agents-and-skills.md). Agents are configurations the one
// conversation switches into; the things worth pinning are the invariants the turn loop leans on —
// a fallback that always exists, keys that stay unique, role gates that actually gate, and route
// selection that cannot seed a conversation with an agent the caller could not have chosen.
public sealed class AgentCatalogueTests
{
    [Fact]
    public void Keys_areUnique_andLowercase()
    {
        // CapabilityKey is persisted on conversations and matched case-insensitively everywhere;
        // two agents behind one key would make the conversation's agent ambiguous.
        var keys = AgentCatalogue.All.Select(agent => agent.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(keys, key => Assert.Equal(key.ToLowerInvariant(), key));
    }

    [Fact]
    public void Find_isCaseInsensitive_andNullSafe()
    {
        Assert.Same(AgentCatalogue.Commercial, AgentCatalogue.Find("COMMERCIAL"));
        Assert.Null(AgentCatalogue.Find(null));
        Assert.Null(AgentCatalogue.Find(""));
        Assert.Null(AgentCatalogue.Find("no-such-agent"));
    }

    [Fact]
    public void ForRoute_picksTheContextualAgent_forAVariationsPage()
    {
        var agent = AgentCatalogue.ForRoute(
            "/projects/abc123/variations/def456", new[] { Role.QuantitySurveyor });

        Assert.Equal("commercial", agent.Key);
    }

    [Fact]
    public void ForRoute_fallsBackToTheOrchestrator_offAnyMatchedRoute()
    {
        var agent = AgentCatalogue.ForRoute("/home", new[] { Role.ManagingDirector });

        Assert.Same(AgentCatalogue.Orchestrator, agent);
    }

    [Fact]
    public void ForRoute_neverSeedsAnAgent_theCallerMayNotEngage()
    {
        // A route match is not a capability. A role outside an agent's AvailableTo falls back to
        // the orchestrator rather than being seeded into an agent it could not have chosen.
        var agent = AgentCatalogue.ForRoute(
            "/projects/abc123/variations/def456", new[] { Role.Subcontractor });

        Assert.Same(AgentCatalogue.Orchestrator, agent);
    }

    [Fact]
    public void Admin_mayEngageEveryAgent()
    {
        // Mirrors SignedInUserResolver granting administrators every role.
        foreach (var agent in AgentCatalogue.All)
            Assert.True(AgentCatalogue.CanEngage(agent, new[] { Role.Admin }));
    }

    [Fact]
    public void EveryAgent_carriesThePromptMechanics()
    {
        // The prompt fragment and description are what the model steers by — an agent shipping
        // without them would be a silent configuration, not a safe one.
        foreach (var agent in AgentCatalogue.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(agent.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(agent.Description));
            Assert.False(string.IsNullOrWhiteSpace(agent.PromptFragment));
            Assert.NotEmpty(agent.AvailableTo);
        }
    }
}
