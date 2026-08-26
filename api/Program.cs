using Azure.Communication.Email;
using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.AccessRequests;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.ArchitectInstructions;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Auth;
using Jewel.JPMS.Api.Features.Boq;
using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Api.Features.Cashflow;
using Jewel.JPMS.Api.Features.Clients;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Api.Features.CostCenters;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Closeout;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Api.Features.CommercialInputs;
using Jewel.JPMS.Api.Features.Cvr;
using Jewel.JPMS.Api.Features.Directory;
using Jewel.JPMS.Api.Features.DocumentControl;
using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Api.Features.Hs;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Registers;
using Jewel.JPMS.Api.Features.Lads;
using Jewel.JPMS.Api.Features.Leads;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.Mobilisation;
using Jewel.JPMS.Api.Features.Places;
using Jewel.JPMS.Api.Features.Platform;
using Jewel.JPMS.Api.Features.Portal;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Api.Features.TenderEnquiries;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Retention;
using Jewel.JPMS.Api.Features.ProjectContracts;
using Jewel.JPMS.Api.Features.Projects;
using Jewel.JPMS.Api.Features.Rates;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Api.Features.Site;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.UsefulInformation;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Api.Gates;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(worker =>
    {
        // Every HTTP response carries the deploy's build number, so an open tab built by an
        // earlier deploy finds out from its next data fetch — see VersionStampMiddleware.
        worker.UseMiddleware<VersionStampMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString application setting missing.");

        services.AddDbContext<JpmsContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
            {
                sqlServer.EnableRetryOnFailure();
                // Cap any single command under the Static Web Apps managed-functions gateway
                // timeout (~45s) so a slow query fails fast with a catchable error instead of
                // hanging the whole request toward a platform 504.
                sqlServer.CommandTimeout(25);
            }));

        // Singleton on purpose: the resolved-caller cache has to outlive the request scope or it
        // caches nothing. Short TTL plus explicit invalidation on permission change — see the type.
        services.AddSingleton<SignedInUserCache>();
        services.AddScoped<SessionManager>();
        services.AddScoped<SignedInUserResolver>();
        services.AddScoped<InviteDirectoryWriter>();
        services.AddScoped<UserInviter>();
        services.AddScoped<PasswordResetSender>();
        RegisterInviteNotifier(services, context.Configuration);
        services.AddDirectoryFeature();
        services.AddAccessRequestsFeature();
        services.AddProjectsFeature();
        services.AddProjectContractsFeature(context.Configuration);
        services.AddClientsFeature();
        services.AddArchitectsFeature();
        services.AddPartiesFeature();
        services.AddLeadsFeature();
        services.AddBoqFeature();
        services.AddRatesFeature();
        services.AddDrawingsFeature(context.Configuration);
        services.AddDocumentControlFeature(context.Configuration);
        services.AddProgressFeature(context.Configuration);
        services.AddProcurementFeature(context.Configuration);
        services.AddTenderEnquiriesFeature(context.Configuration);
        services.AddLocalSearchFeature(context.Configuration);
        services.AddVariationsFeature();
        services.AddSubcontractorsFeature(context.Configuration);
        services.AddPortalFeature();
        services.AddHsFeature();
        services.AddMobilisationFeature();
        services.AddSiteFeature();
        services.AddCommercialFeature();
        services.AddLabourFeature();
        services.AddRegistersFeature();
        services.AddCommercialInputsFeature();
        services.AddRetentionFeature();
        services.AddCashflowFeature();
        services.AddValuationInvoicesFeature();
        services.AddCvrFeature();
        services.AddCloseoutFeature();
        services.AddRequestsFeature(context.Configuration);
        services.AddArchitectInstructionsFeature(context.Configuration);
        services.AddRecordLinksFeature();
        services.AddAuditFeature();
        services.AddTodosFeature();
        services.AddUsefulInformationFeature();
        services.AddLadsFeature();
        services.AddCostCentersFeature();
        services.AddMailboxIntakeFeature(context.Configuration);
        services.AddAiFeature(context.Configuration);
        services.AddXeroFeature(context.Configuration);
        services.AddPlatformFeature();
    })
    .Build();

// No automatic migration on start-up, deliberately. Schema changes are applied by hand from a
// reviewed script (see docs/09-operations/applying-migrations.md) — the API only ever reads and
// writes rows, never alters the schema. Two reasons this is not a convenience worth having:
//
//   1. Safety. Migrating from here means whichever managed-function instance happens to cold-start
//      first after a deploy applies the schema change, unreviewed and unwatched, and the old catch
//      block swallowed any failure. EF Core 8 has no migration lock (that arrived in EF Core 9), so
//      two instances scaling up together could both attempt it.
//   2. Speed. It sat in front of host.RunAsync(), so every cold start built the full 117-entity
//      model and made a round trip to SQL before a single endpoint would answer — and with
//      EnableRetryOnFailure() a momentarily slow database turned that into minutes of dead API.
//
// If the schema is behind the code, endpoints touching the new columns will fail loudly, which is
// the intended behaviour: a visible error beats a silent self-modifying database.
await host.RunAsync();

static void RegisterInviteNotifier(IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration["CommunicationServicesConnectionString"];
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        services.AddScoped<IInviteNotifier, LoggingInviteNotifier>();
        return;
    }

    var senderAddress = configuration["InviteEmailSender"] ?? InviteSettings.DefaultSenderAddress;
    services.AddSingleton(new EmailClient(connectionString));
    services.AddScoped<IInviteNotifier>(provider =>
        new AzureEmailInviteNotifier(provider.GetRequiredService<EmailClient>(), senderAddress));
}
