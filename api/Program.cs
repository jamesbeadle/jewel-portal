using Azure.Communication.Email;
using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.AccessRequests;
using Jewel.JPMS.Api.Features.Agents;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Auth;
using Jewel.JPMS.Api.Features.Boq;
using Jewel.JPMS.Api.Features.Cashflow;
using Jewel.JPMS.Api.Features.CostCenters;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Closeout;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Api.Features.CommercialInputs;
using Jewel.JPMS.Api.Features.Cvr;
using Jewel.JPMS.Api.Features.Directory;
using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Api.Features.Hs;
using Jewel.JPMS.Api.Features.Leads;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.Mobilisation;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Api.Features.Projects;
using Jewel.JPMS.Api.Features.Rates;
using Jewel.JPMS.Api.Features.Site;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Api.Gates;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString application setting missing.");

        services.AddDbContext<JpmsContext>(options =>
            options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure()));

        services.AddScoped<SessionManager>();
        services.AddScoped<SignedInUserResolver>();
        services.AddScoped<InviteDirectoryWriter>();
        services.AddScoped<UserInviter>();
        RegisterInviteNotifier(services, context.Configuration);
        services.AddDirectoryFeature();
        services.AddAccessRequestsFeature();
        services.AddProjectsFeature();
        services.AddLeadsFeature();
        services.AddBoqFeature();
        services.AddRatesFeature();
        services.AddDrawingsFeature();
        services.AddProcurementFeature();
        services.AddSubcontractorsFeature();
        services.AddHsFeature();
        services.AddMobilisationFeature();
        services.AddSiteFeature();
        services.AddCommercialFeature();
        services.AddCommercialInputsFeature();
        services.AddCashflowFeature();
        services.AddCvrFeature();
        services.AddCloseoutFeature();
        services.AddRequestsFeature();
        services.AddAgentsFeature();
        services.AddCostCentersFeature();
        services.AddMailboxIntakeFeature(context.Configuration);
        services.AddAiFeature(context.Configuration);
    })
    .Build();

await ApplyDatabaseMigrations(host.Services);

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

static async Task ApplyDatabaseMigrations(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<JpmsContext>();
        await context.Database.MigrateAsync();
    }
    catch (Exception migrationError)
    {
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration")
            .LogError(migrationError, "Startup database migration failed; the host will continue and retry on the next start.");
    }
}
