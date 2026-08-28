using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Mcp;

// The MCP endpoint that lets team members use the portal from their own AI tools (Claude,
// Perplexity, Claude Code) on their own subscriptions. Auth is the Connect feature's OAuth
// tokens; the tool surface is the AiToolCatalogue, role-filtered per user exactly as the
// endpoints gate. See docs/ai/10-mcp-connector.md.
public static class McpFeatureRegistration
{
    public static IServiceCollection AddMcpFeature(this IServiceCollection services)
    {
        return services;
    }
}
