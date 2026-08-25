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
        // Fails the boot if the hand-kept AI registries (tools, dialogs, labels, descriptions)
        // have drifted apart — deterministic static data, so a throw here can only mean a commit
        // shipped the drift, and stopping it at startup beats the assistant narrating actions
        // that never happen.
        AiRegistryDriftCheck.Assert();

        var options = AnthropicOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            // Own HttpClient instances so they don't clash with the Graph client's registration.
            services.AddSingleton<IClaudeClient>(sp =>
                new ClaudeClient(new HttpClient(), options, sp.GetRequiredService<ILogger<ClaudeClient>>()));

            services.AddSingleton<IClaudeConversationClient>(sp =>
                new ClaudeConversationClient(
                    // The client manages its own per-attempt deadlines against a 36s budget (see
                    // ClaudeConversationClient.CallBudget) so a hop always answers inside the
                    // Static Web Apps gateway's ~45s. This outer timeout is only the backstop for
                    // a hung socket the budget's linked cancellation somehow failed to cut.
                    new HttpClient { Timeout = TimeSpan.FromSeconds(60) },
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

        // Chat attachments: bytes kept in the ai-attachments container so any part can be read on
        // demand, a manifest + preview persisted as a Context row. No Claude call at upload.
        RegisterAttachmentStore(services, configuration);
        services.AddScoped<ICommandHandler<AddAiAttachment, AiAttachmentReceipt>, AddAiAttachmentHandler>();
        services.AddScoped<AddAiAttachmentValidation>();

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

    private static void RegisterAttachmentStore(IServiceCollection services, IConfiguration configuration)
    {
        // Its own setting when one is configured, otherwise the same storage account the other
        // stores share (the Document Control / drawings pattern): a dedicated private container.
        var connectionString = configuration["AiAttachmentStorage:ConnectionString"]
            ?? configuration["DocumentControlStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<Storage.IAiAttachmentStore, Storage.NullAiAttachmentStore>();
        else
            services.AddSingleton<Storage.IAiAttachmentStore>(_ => new Storage.AzureBlobAiAttachmentStore(connectionString));
    }
}
