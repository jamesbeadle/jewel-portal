using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Ai.Commands;
using Jewel.JPMS.Api.Features.Ai.Queries;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Registers the Anthropic (Claude) clients and the assistant's turn endpoint. Real clients when an
/// API key is present in configuration, otherwise no-ops so the rest of the app runs unchanged —
/// the triage draft falls back to plain subject/body, and the assistant says plainly that it is not
/// connected. The key is read from app settings / Key Vault only (Anthropic__ApiKey) — never from
/// source control.
/// </summary>
public static class AiFeatureRegistration
{
    public static IServiceCollection AddAiFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = AnthropicOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            // Own HttpClient instances so they don't clash with the Graph client's registration.
            services.AddSingleton<IClaudeClient>(sp =>
                new ClaudeClient(new HttpClient(), options, sp.GetRequiredService<ILogger<ClaudeClient>>()));

            services.AddSingleton<IClaudeConversationClient>(sp =>
                new ClaudeConversationClient(
                    // The turn loop makes several calls inside one request; give it a per-call
                    // timeout well under the Static Web Apps gateway's ~45s so one slow call fails
                    // fast rather than taking the whole turn down with it.
                    new HttpClient { Timeout = TimeSpan.FromSeconds(25) },
                    options,
                    sp.GetRequiredService<ILogger<ClaudeConversationClient>>()));
        }
        else
        {
            services.AddSingleton<IClaudeClient, NullClaudeClient>();
            services.AddSingleton<IClaudeConversationClient, NullClaudeConversationClient>();
        }

        // Who is talking to the assistant on this invocation — set by the endpoint after the auth
        // gate, because the tool layer filters on the caller's roles, not just their email.
        services.AddScoped<AiCaller>();

        // One hop of a turn. Shared by the first hop and every continuation so they cannot diverge.
        services.AddScoped<AiTurnRunner>();

        services.AddScoped<ICommandHandler<SendAiMessage, AiTurnResult>, SendAiMessageHandler>();
        services.AddScoped<SendAiMessageAuthorisation>();
        services.AddScoped<SendAiMessageValidation>();

        services.AddScoped<ICommandHandler<ContinueAiTurn, AiTurnResult>, ContinueAiTurnHandler>();
        services.AddScoped<ContinueAiTurnAuthorisation>();
        services.AddScoped<ContinueAiTurnValidation>();

        services.AddScoped<IQueryHandler<ListAiConversation, IReadOnlyList<AiChatMessage>>, ListAiConversationHandler>();

        // The agent activity log. Scoped because it writes through the request's JpmsContext.
        services.AddScoped<AgentActivityLog>();
        services.AddScoped<IQueryHandler<ListAgentActivity, IReadOnlyList<AgentActivity>>, ListAgentActivityHandler>();

        // The skill store — the domain half of an agent, edited in the portal
        // (docs/ai/05-agents-and-skills.md §2).
        services.AddScoped<IQueryHandler<ListAiSkills, IReadOnlyList<SkillSummary>>, Skills.ListAiSkillsHandler>();
        services.AddScoped<IQueryHandler<GetAiSkill, SkillDetail?>, Skills.GetAiSkillHandler>();
        services.AddScoped<ICommandHandler<SaveAiSkill, Acknowledgement>, Skills.SaveAiSkillHandler>();
        services.AddScoped<Skills.SaveAiSkillAuthorisation>();
        services.AddScoped<Skills.SaveAiSkillValidation>();
        services.AddScoped<ICommandHandler<SaveAiSkillReference, Acknowledgement>, Skills.SaveAiSkillReferenceHandler>();
        services.AddScoped<Skills.SaveAiSkillReferenceAuthorisation>();
        services.AddScoped<Skills.SaveAiSkillReferenceValidation>();

        return services;
    }
}
