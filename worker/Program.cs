using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Bluebeam;
using Jewel.JPMS.Api.Features.Bluebeam.Extraction;
using Jewel.JPMS.Api.Features.Sales.Imagine;
using Jewel.JPMS.Api.Features.Sales.Research;
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
        // Application Insights for the ISOLATED process. Without these two lines every ILogger
        // call inside function code goes nowhere — only host-level telemetry ever reached the
        // portal, which made worker failures undiagnosable (learned 2026-08-31, Bluebeam connect).
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

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
        services.AddScoped<BluebeamConnectionWriter>();
        services.AddScoped<DrawingExtractionRunner>();
        services.AddScoped<DrawingExtractionResultWriter>();

        // Sales strategy research (2026-09-06): the queue consumer that turns a brief into an
        // evidenced strategy — Claude with web search, then the approach plan. Same Anthropic
        // options as the SWA API (app setting Anthropic__ApiKey, identical name); without a key
        // the runner stamps every run Failed with that reason rather than crashing the host.
        var anthropicOptions = AnthropicOptions.FromConfiguration(context.Configuration);
        services.AddSingleton(anthropicOptions);
        if (anthropicOptions.IsConfigured)
            services.AddSingleton<IClaudeClient>(sp =>
                new ClaudeClient(new HttpClient(), anthropicOptions, sp.GetRequiredService<ILogger<ClaudeClient>>()));
        else
            services.AddSingleton<IClaudeClient, NullClaudeClient>();
        services.AddSingleton(sp => new StrategyResearcher(
            new HttpClient { Timeout = TimeSpan.FromMinutes(6) }, anthropicOptions, sp.GetRequiredService<ILogger<StrategyResearcher>>()));
        services.AddScoped<StrategyResearchRunner>();

        // Imagine renders (2026-09-06): the queue consumer behind the public /imagine page — Claude
        // reads the prospect's photos and writes the concepts (Anthropic__ApiKey, as above), Azure
        // OpenAI gpt-image-1 renders each over their photo (AzureImage__Endpoint / ApiKey /
        // Deployment), the renders land in the "imagine" blob container (ImagineStorage:
        // ConnectionString, else AzureWebJobsStorage — the api resolves identically), and the
        // prospect is emailed through ACS (CommunicationServicesConnectionString, with
        // PublicSiteUrl and SalesMailbox:Address for the link and the Reply-To). Every piece has a
        // null stand-in: without it the round is stamped Failed with the reason, never a crash.
        services.AddSingleton(sp => new ImagineConceptWriter(
            new HttpClient { Timeout = TimeSpan.FromMinutes(4) }, anthropicOptions, sp.GetRequiredService<ILogger<ImagineConceptWriter>>()));
        var azureImageOptions = AzureImageOptions.FromConfiguration(context.Configuration);
        services.AddSingleton(azureImageOptions);
        if (azureImageOptions.IsConfigured)
            services.AddSingleton<IAzureImageClient>(sp => new AzureImageClient(
                new HttpClient { Timeout = TimeSpan.FromMinutes(4) }, azureImageOptions, sp.GetRequiredService<ILogger<AzureImageClient>>()));
        else
            services.AddSingleton<IAzureImageClient, NullAzureImageClient>();
        var imagineStorage = context.Configuration["ImagineStorage:ConnectionString"]
            ?? context.Configuration["AzureWebJobsStorage"];
        if (string.IsNullOrWhiteSpace(imagineStorage))
            services.AddSingleton<IImagineImageStore, NullImagineImageStore>();
        else
            services.AddSingleton<IImagineImageStore>(_ => new AzureBlobImagineImageStore(imagineStorage!));
        var notifierOptions = ImagineNotifierOptions.FromConfiguration(context.Configuration);
        services.AddSingleton(notifierOptions);
        var acsConnection = context.Configuration["CommunicationServicesConnectionString"];
        if (string.IsNullOrWhiteSpace(acsConnection))
            services.AddSingleton<IImagineNotifier, NullImagineNotifier>();
        else
            services.AddSingleton<IImagineNotifier>(sp => new AcsImagineNotifier(
                new Azure.Communication.Email.EmailClient(acsConnection!), notifierOptions, sp.GetRequiredService<ILogger<AcsImagineNotifier>>()));
        services.AddScoped<ImagineRenderRunner>();

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
