using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Bluebeam;
using Jewel.JPMS.Api.Features.Bluebeam.Extraction;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;
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

        // Xero: the nightly ledger sync + auto-allocation timer (Xero/XeroNightlyWorker.cs) reuses
        // the API's own handlers, compiled in via linked source. Real client when the custom
        // connection's credentials are configured (app settings Xero__ClientId / Xero__ClientSecret,
        // same names as the SWA API), otherwise the no-op client — the timer then logs that Xero is
        // not configured and does nothing, so the host always starts. Singleton client so the cached
        // snapshot and access token are shared across invocations, mirroring the API's registration.
        var xeroOptions = XeroOptions.FromConfiguration(context.Configuration);
        services.AddSingleton(xeroOptions);
        if (xeroOptions.IsConfigured)
        {
            services.AddSingleton<IXeroClient>(sp =>
                new XeroClient(new HttpClient(), xeroOptions, sp.GetRequiredService<ILogger<XeroClient>>()));
        }
        else
        {
            services.AddSingleton<IXeroClient, NullXeroClient>();
        }
        services.AddScoped<IXeroWriteBackService, XeroWriteBackService>();
        services.AddScoped<ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult>, SyncXeroLedgerHandler>();
        services.AddScoped<ICommandHandler<AllocateSuggestedXeroLines, int>, AllocateSuggestedXeroLinesHandler>();
        // Site P&L refresh (Profit Summary's cumulative chart) — same handler as the API's
        // explicit sync endpoint, run nightly after the ledger sync.
        services.AddScoped<ICommandHandler<SyncXeroSitePnl, XeroSitePnlSyncResult>,
            Jewel.JPMS.Api.Features.Xero.SitePnl.SyncXeroSitePnlHandler>();

        // Bluebeam: the drawing-extraction queue consumer + the nightly refresh-token keep-alive.
        // Same options/client fork as the SWA API (app settings Bluebeam__ClientId /
        // Bluebeam__ClientSecret, identical names); the runner and its result writer are scoped
        // because they write through JpmsContext.
        var bluebeamOptions = BluebeamOptions.FromConfiguration(context.Configuration);
        services.AddSingleton(bluebeamOptions);
        if (bluebeamOptions.IsConfigured)
        {
            services.AddSingleton<IBluebeamClient>(sp =>
                new BluebeamClient(new HttpClient(), bluebeamOptions, sp.GetRequiredService<ILogger<BluebeamClient>>()));
        }
        else
        {
            services.AddSingleton<IBluebeamClient, NullBluebeamClient>();
        }
        services.AddScoped<BluebeamTokenService>();
        services.AddScoped<DrawingExtractionRunner>();
        services.AddScoped<DrawingExtractionResultWriter>();

        // The drawings blob store — the extraction reads revision PDFs and writes the payload
        // blobs beside them. Same connection resolution as DrawingsFeatureRegistration in the api.
        var drawingsStorage = context.Configuration["DrawingsStorage:ConnectionString"]
            ?? context.Configuration["AzureWebJobsStorage"];
        if (string.IsNullOrWhiteSpace(drawingsStorage))
        {
            services.AddSingleton<IDrawingBlobStore, NullDrawingBlobStore>();
        }
        else
        {
            services.AddSingleton<IDrawingBlobStore>(_ => new AzureBlobDrawingStore(drawingsStorage!));
        }
    })
    .Build();

await host.RunAsync();
