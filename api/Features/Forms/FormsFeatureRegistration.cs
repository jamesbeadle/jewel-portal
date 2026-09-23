using Azure.Communication.Email;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Api.Features.Forms.Office.Submissions;
using Jewel.JPMS.Api.Features.Forms.Public;
using Jewel.JPMS.Api.Features.Forms.Registers;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Forms;

/// <summary>
/// The forms ported from Jeremy's forms dashboard (2026-09-23): the public pages' API, the office's commands
/// and queries, the three restricted stores and the mail. Every outside dependency has a null
/// stand-in, so the API always starts and a missing setting is refused with its reason.
/// </summary>
public static class FormsFeatureRegistration
{
    public static IServiceCollection AddFormsFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = FormSiteOptions.FromConfiguration(configuration);
        services.AddSingleton(options);
        AddStore(services, configuration);
        AddMailer(services, configuration, options);
        services.AddScoped<PublicFormService>();
        services.AddScoped<FormSubmissionAccess>();
        services.AddScoped<RightToWorkEvidenceFiling>();
        services.AddScoped<FormRegisterValidations>();
        return services.AddFormLinkCommands().AddFormSubmissionCommands().AddFormRegisterCommands();
    }

    private static void AddStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["FormStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IFormEvidenceStore, NullFormEvidenceStore>();
            return;
        }
        services.AddSingleton<IFormEvidenceStore>(_ => new AzureFormEvidenceStore(connectionString));
    }

    private static void AddMailer(IServiceCollection services, IConfiguration configuration, FormSiteOptions options)
    {
        var acsConnection = configuration["CommunicationServicesConnectionString"];
        if (string.IsNullOrWhiteSpace(acsConnection))
        {
            services.AddSingleton<IFormMailer, NullFormMailer>();
            return;
        }
        services.AddSingleton<IFormMailer>(provider =>
            new AcsFormMailer(new EmailClient(acsConnection), options, provider.GetRequiredService<ILogger<AcsFormMailer>>()));
    }
}
