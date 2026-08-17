using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake;

/// <summary>
/// Registers the producer/webhook side of the projects@ mailbox-intake feature that runs inside the
/// Static Web Apps managed Functions API. SWA managed functions only support HTTP triggers, so the
/// background workers (delta sweep, subscription renewal, queue consumers, folder/outbound actions)
/// live in the standalone <c>worker</c> Function App instead. This registration wires only what the
/// API needs: the options, the queue producer, and the triage action scheduler. The HTTP webhook
/// endpoint is discovered automatically as a Function.
///
/// The two apps communicate over Storage Queues. Both must point <c>MailboxQueuesConnection</c> at
/// the same storage account so the API's enqueues are consumed by the worker's queue triggers.
/// The client secret is read from configuration (app settings / Key Vault) only — never from source.
/// </summary>
public static class MailboxIntakeFeatureRegistration
{
    public static IServiceCollection AddMailboxIntakeFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = MailboxIntakeOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        // Queue producer: the webhook enqueues notifications and the triage handlers enqueue
        // mailbox actions onto the shared storage account. Falls back to AzureWebJobsStorage for
        // local dev; in production set MailboxQueuesConnection to the account the worker also uses.
        var queueConnection = configuration["MailboxQueuesConnection"]
            ?? configuration["AzureWebJobsStorage"];
        if (options.Enabled && !string.IsNullOrWhiteSpace(queueConnection))
        {
            services.AddSingleton<IMailboxQueue>(sp =>
                new StorageMailboxQueue(queueConnection!, sp.GetRequiredService<ILogger<StorageMailboxQueue>>()));
        }
        else
        {
            services.AddSingleton<IMailboxQueue, NullMailboxQueue>();
        }

        // Always available so the triage handlers can depend on it; it self-gates on the flags.
        services.AddSingleton<IMailboxActionScheduler, MailboxActionScheduler>();

        // Large-attachment share links: files that would push an email past the ~25 MB Exchange
        // ceiling are copied into a private 'email-shares' container and sent as 7-day SAS download
        // links instead (EmailAttachmentPlanner decides the split; the invite-draft, compose and
        // request-draft handlers consume it). Shares the drawings storage account — same fallback
        // chain as the drawings blob store — and the null store keeps callers on their pre-link
        // behaviour when no storage is configured.
        var shareConnection = configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];
        if (string.IsNullOrWhiteSpace(shareConnection))
        {
            services.AddSingleton<Sharing.IEmailFileShareStore, Sharing.NullEmailFileShareStore>();
        }
        else
        {
            services.AddSingleton<Sharing.IEmailFileShareStore>(sp =>
                new Sharing.AzureBlobEmailFileShareStore(
                    shareConnection!,
                    sp.GetRequiredService<ILogger<Sharing.AzureBlobEmailFileShareStore>>()));
        }

        // On-demand intake message reader: lets the triage detail endpoint pull an email's full body
        // + attachment names live from Graph when a triager opens it. Real when Graph credentials are
        // configured for the API app, otherwise a no-op so callers fall back to the stored preview.
        // NOTE: this requires MailboxIntake:TenantId / ClientId / ClientSecret to be present in the
        // SWA API app settings (the client secret is a mailbox password — app settings / Key Vault only).
        if (options.Enabled && options.IsConfigured)
        {
            services.AddSingleton<GraphTokenProvider>();
            services.AddSingleton<HttpClient>();
            services.AddSingleton<IIntakeMessageReader, GraphIntakeMessageReader>();
            // Live-read triage client: list Inbox/General messages and move them. Shares the same
            // app-only token + HttpClient as the on-demand reader.
            services.AddSingleton<IMailboxGraphClient, MailboxGraphClient>();
        }
        else
        {
            services.AddSingleton<IIntakeMessageReader, NullIntakeMessageReader>();
            services.AddSingleton<IMailboxGraphClient, NullMailboxGraphClient>();
        }

        // Inbound body rendering: sanitises an opened email's HTML and embeds its inline images
        // (pasted screenshots) so they show in the portal. Stateless over the reader, so singleton.
        services.AddSingleton<InboundEmailBodyBuilder>();

        // Triage compose (POST mailbox/compose): send — or stage — an email from the projects
        // mailbox. The handler is registered by concrete type too so the multipart endpoint can
        // pass uploaded file bytes alongside the command.
        services.AddSingleton<Compose.ComposeHtmlPipeline>();
        services.AddScoped<Compose.SendMailboxEmailHandler>();
        services.AddScoped<
            Jewel.JPMS.Api.Cqrs.ICommandHandler<
                Jewel.JPMS.Contracts.MailboxCompose.SendMailboxEmail,
                Jewel.JPMS.Contracts.MailboxCompose.ComposeOutcome>>(
            sp => sp.GetRequiredService<Compose.SendMailboxEmailHandler>());

        return services;
    }
}
