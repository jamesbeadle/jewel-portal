using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Connect;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The MCP connector's static contracts (docs/ai/10-mcp-connector.md): the tool catalogue is
// role-filtered exactly as the endpoints gate, the write tools exist for the roles that may use
// them, and the hand-kept registries still agree (the boot drift check). These pin the surface a
// team member's AI tool is offered — a regression here is a tool silently vanishing from, or
// leaking into, someone's Claude.
public sealed class AiConnectorTests
{
    private static SignedInUser UserWith(params Role[] roles) =>
        new("test@jewelbb.co.uk", "Test User", roles);

    [Fact]
    public void DriftCheck_passes()
    {
        // Throws on drift — the same call the boot makes.
        AiRegistryDriftCheck.Assert();
    }

    [Fact]
    public void ToolNames_areUnique()
    {
        var names = AiToolCatalogue.All.Select(tool => tool.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Catalogue_carriesNoUiTools()
    {
        // The chat's browser-executed tools are gone; everything left runs server-side.
        Assert.DoesNotContain(AiToolCatalogue.All, tool => tool.Kind == AiToolKind.Ui);
    }

    [Fact]
    public void ForConnector_filtersByRole()
    {
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();

        Assert.Contains("list_projects", director);
        Assert.Contains("get_request_context", director);
        // A subcontractor contact must not be offered the internal read surface.
        Assert.DoesNotContain("list_projects", subcontractor);
        Assert.DoesNotContain("get_valuation_context", subcontractor);
        Assert.True(director.Count > subcontractor.Count);
    }

    [Fact]
    public void WriteTools_exist_andAreMarkedAsWrites()
    {
        var admin = AiToolCatalogue.ForConnector(UserWith(Role.Admin));
        foreach (var name in new[] { "post_request_message", "add_todo", "complete_todo", "log_todo_progress", "save_skill" })
        {
            var tool = admin.SingleOrDefault(candidate => candidate.Name == name);
            Assert.NotNull(tool);
            Assert.Equal(AiToolKind.Write, tool!.Kind);
        }
    }

    [Fact]
    public void SaveSkill_isNotOfferedOutsideTheSkillGate()
    {
        var quantitySurveyor = AiToolCatalogue.ForConnector(UserWith(Role.QuantitySurveyor)).Select(t => t.Name);
        Assert.DoesNotContain("save_skill", quantitySurveyor);
    }

    [Theory]
    [InlineData("https://claude.ai/api/mcp/auth_callback", true)]
    [InlineData("https://www.perplexity.ai/rest/connections/oauth_callback", true)]
    [InlineData("http://localhost:33418/callback", true)]
    [InlineData("http://127.0.0.1:8976/oauth/callback", true)]
    [InlineData("http://evil.example/callback", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void RedirectUris_allowHttpsAndLoopbackOnly(string uri, bool acceptable)
    {
        Assert.Equal(acceptable, OAuthRedirects.IsAcceptable(uri));
    }
}
