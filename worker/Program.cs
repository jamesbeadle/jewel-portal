using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// The mailbox-intake background workers (timer + queue triggers) live here, in a standalone
// Azure Function App. They cannot run inside the Static Web Apps managed Functions API, which
// only supports HTTP triggers. The SWA API keeps the HTTP webhook + the triage-side producers;
// this worker owns ingestion, the delta sweep, subscription renewal, and folder/outbound actions.
//
// This app does NOT apply EF migrations — the SWA API owns the schema. The worker shares the
// identical JpmsContext (via linked source) and only reads/updates existing tables.
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString application setting missing.");

        services.AddDbContext<JpmsContext>(options =>
            options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure()));

        var mailboxOptions = MailboxIntakeOptions.FromConfiguration(context.Configuration);
        services.AddSingleton(mailboxOptions);

        // Graph client: real when configured, otherwise a logged no-op (so the host always starts).
        // IGraphMailClient creates the outbound document drafts; the worker never reads mail — the
        // document is built from SQL alone, so no read-by-tag client is wired here.
        if (mailboxOptions.Enabled && mailboxOptions.IsConfigured)
        {
            services.AddSingleton<GraphTokenProvider>();
            services.AddSingleton<HttpClient>();
            services.AddSingleton<IGraphMailClient, GraphMailClient>();
        }
        else
        {
            services.AddSingleton<IGraphMailClient, NullGraphMailClient>();
        }

        // Queue producer + action scheduler: ingestion auto-link enqueues a folder move onto the
        // same mailbox-actions queue the MailboxActionWorker consumes. Mirrors the SWA API wiring;
        // both apps must point MailboxQueuesConnection at the same storage account.
        var queueConnection = context.Configuration["MailboxQueuesConnection"]
            ?? context.Configuration["AzureWebJobsStorage"];
        if (mailboxOptions.Enabled && !string.IsNullOrWhiteSpace(queueConnection))
        {
            services.AddSingleton<IMailboxQueue>(sp =>
                new StorageMailboxQueue(queueConnection!, sp.GetRequiredService<ILogger<StorageMailboxQueue>>()));
        }
        else
        {
            services.AddSingleton<IMailboxQueue, NullMailboxQueue>();
        }
        services.AddSingleton<IMailboxActionScheduler, MailboxActionScheduler>();
    })
    .Build();

await host.RunAsync();
