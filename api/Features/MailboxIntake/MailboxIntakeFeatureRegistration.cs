using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Ingestion;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Jewel.JPMS.Api.Features.MailboxIntake.Subscriptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake;

/// <summary>
/// Registers the projects@ mailbox-intake feature. Mirrors the conditional invite-notifier
/// pattern: when the Graph credentials are present the real services are wired; otherwise logged
/// no-op fallbacks are used so the host always starts and the rest of the app runs unaffected.
/// The client secret is read from configuration (app settings / Key Vault) only — never from source.
/// </summary>
public static class MailboxIntakeFeatureRegistration
{
    public static IServiceCollection AddMailboxIntakeFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = BindOptions(configuration);
        services.AddSingleton(options);

        // Graph client: real when configured, otherwise a logged no-op.
        if (options.Enabled && options.IsConfigured)
        {
            services.AddSingleton<GraphTokenProvider>();
            services.AddSingleton<HttpClient>();
            services.AddSingleton<IGraphMailClient, GraphMailClient>();
        }
        else
        {
            services.AddSingleton<IGraphMailClient, NullGraphMailClient>();
        }

        // Queue: real Storage queue when a storage connection is present, otherwise a no-op.
        var storage = configuration["AzureWebJobsStorage"];
        if (options.Enabled && !string.IsNullOrWhiteSpace(storage))
        {
            services.AddSingleton<IMailboxQueue>(sp =>
                new StorageMailboxQueue(storage!, sp.GetRequiredService<ILogger<StorageMailboxQueue>>()));
        }
        else
        {
            services.AddSingleton<IMailboxQueue, NullMailboxQueue>();
        }

        services.AddScoped<MailboxSyncStateStore>();
        services.AddScoped<IntakeIngestionService>();
        services.AddScoped<MailboxSubscriptionManager>();

        // Always available so the triage handlers can depend on it; it self-gates on the flags.
        services.AddSingleton<IMailboxActionScheduler, MailboxActionScheduler>();

        return services;
    }

    private static MailboxIntakeOptions BindOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(MailboxIntakeOptions.SectionName);
        var options = new MailboxIntakeOptions
        {
            TenantId = section["TenantId"],
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            NotificationUrl = section["NotificationUrl"],
            ClientState = section["ClientState"],
            Enabled = ParseBool(section["Enabled"], true),
            EnableDeltaSweep = ParseBool(section["EnableDeltaSweep"], true),
            EnableWebhook = ParseBool(section["EnableWebhook"], false),
            EnableFolderMoves = ParseBool(section["EnableFolderMoves"], true),
            EnableOutboundSend = ParseBool(section["EnableOutboundSend"], false)
        };

        var mailbox = section["Mailbox"];
        if (!string.IsNullOrWhiteSpace(mailbox))
            options.Mailbox = mailbox;

        options.Folders.InProgress = section["Folders:InProgress"];
        options.Folders.Logged = section["Folders:Logged"];
        options.Folders.NotActioned = section["Folders:NotActioned"];
        options.Folders.NeedsAttention = section["Folders:NeedsAttention"];

        return options;
    }

    private static bool ParseBool(string? value, bool fallback) =>
        bool.TryParse(value, out var parsed) ? parsed : fallback;
}
