using Azure.Communication.Email;
using Jewel.JPMS.Api.Features.Forms;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Api.Features.Forms.Retention;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Worker.Forms;

/// <summary>
/// The forms' nightly work, on the api's own code (linked source): the same three stores, the same
/// mail and the same link options, resolved from the same settings, so a destruction or a chase here
/// is exactly what the api would do.
/// </summary>
public static class FormsWorkerRegistration
{
    public static IServiceCollection AddFormsWork(this IServiceCollection services, IConfiguration configuration)
    {
        var options = FormSiteOptions.FromConfiguration(configuration);
        services.AddSingleton(options);
        var storage = configuration["FormStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];
        var hasStorage = !string.IsNullOrWhiteSpace(storage);
        services.AddSingleton<IFormEvidenceStore>(_ => hasStorage ? new AzureFormEvidenceStore(storage!) : new NullFormEvidenceStore());
        var acsConnection = configuration["CommunicationServicesConnectionString"];
        var hasMail = !string.IsNullOrWhiteSpace(acsConnection);
        services.AddSingleton<IFormMailer>(provider => hasMail
            ? new AcsFormMailer(new EmailClient(acsConnection), options, provider.GetRequiredService<ILogger<AcsFormMailer>>())
            : new NullFormMailer());
        services.AddScoped<FormRetentionSweep>();
        services.AddScoped<FormRenewalChase>();
        return services;
    }
}
