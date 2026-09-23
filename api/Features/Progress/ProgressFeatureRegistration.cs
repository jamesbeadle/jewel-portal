using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Queries;
using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Api.Features.Progress.SitePhotos;
using Jewel.JPMS.Api.Features.Progress.Queries;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Progress;

public static class ProgressFeatureRegistration
{
    public static IServiceCollection AddProgressFeature(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterPhotoStore(services, configuration);

        services.AddScoped<IQueryHandler<ListProgressUpdatesForProject, IReadOnlyList<ProgressUpdate>>, ListProgressUpdatesForProjectHandler>();
        services.AddScoped<IQueryHandler<ListProgressReportsForProject, IReadOnlyList<ProgressReport>>, ListProgressReportsForProjectHandler>();

        services.AddScoped<ProgressPhotoIntake>();

        services.AddScoped<ICommandHandler<CreateProgressUpdate, ProgressUpdate>, CreateProgressUpdateHandler>();
        services.AddScoped<CreateProgressUpdateAuthorisation>();
        services.AddScoped<CreateProgressUpdateValidation>();

        services.AddScoped<ICommandHandler<CreateProgressUpdateWithPhotos, ProgressUpdate>, CreateProgressUpdateWithPhotosHandler>();
        services.AddScoped<CreateProgressUpdateWithPhotosAuthorisation>();
        services.AddScoped<CreateProgressUpdateWithPhotosValidation>();

        services.AddScoped<ICommandHandler<AddProgressPhotos, ProgressUpdate>, AddProgressPhotosHandler>();
        services.AddScoped<AddProgressPhotosAuthorisation>();
        services.AddScoped<AddProgressPhotosValidation>();

        services.AddScoped<ICommandHandler<UpdateProgressUpdate, ProgressUpdate>, UpdateProgressUpdateHandler>();
        services.AddScoped<UpdateProgressUpdateAuthorisation>();
        services.AddScoped<UpdateProgressUpdateValidation>();

        services.AddScoped<ICommandHandler<DeleteProgressUpdate, Acknowledgement>, DeleteProgressUpdateHandler>();
        services.AddScoped<DeleteProgressUpdateAuthorisation>();

        services.AddScoped<ICommandHandler<DeleteProgressPhoto, Acknowledgement>, DeleteProgressPhotoHandler>();
        services.AddScoped<DeleteProgressPhotoAuthorisation>();

        services.AddScoped<ICommandHandler<CreateProgressReport, ProgressReport>, CreateProgressReportHandler>();
        services.AddScoped<CreateProgressReportAuthorisation>();
        services.AddScoped<CreateProgressReportValidation>();

        services.AddScoped<ICommandHandler<UpdateProgressReport, ProgressReport>, UpdateProgressReportHandler>();
        services.AddScoped<UpdateProgressReportAuthorisation>();
        services.AddScoped<UpdateProgressReportValidation>();

        services.AddScoped<ICommandHandler<DeleteProgressReport, Acknowledgement>, DeleteProgressReportHandler>();
        services.AddScoped<DeleteProgressReportAuthorisation>();

        AddContractorsReports(services);
        services.AddSitePhotos();
        return services;
    }

    private static void AddContractorsReports(IServiceCollection services)
    {
        services.AddScoped<ContractorsReportComposer>();
        services.AddScoped<ContractorsReportPhotoLoader>();
        services.AddScoped<ContractorsReportBuilder>();

        services.AddScoped<IQueryHandler<ListContractorsReports, IReadOnlyList<ContractorsReport>>, ListContractorsReportsHandler>();
        services.AddScoped<IQueryHandler<GetContractorsReport, ContractorsReportView?>, GetContractorsReportHandler>();

        services.AddScoped<ICommandHandler<CreateContractorsReport, ContractorsReport>, CreateContractorsReportHandler>();
        services.AddScoped<CreateContractorsReportAuthorisation>();
        services.AddScoped<CreateContractorsReportValidation>();

        services.AddScoped<ICommandHandler<UpdateContractorsReport, ContractorsReport>, UpdateContractorsReportHandler>();
        services.AddScoped<UpdateContractorsReportAuthorisation>();
        services.AddScoped<UpdateContractorsReportValidation>();

        services.AddScoped<ICommandHandler<DeleteContractorsReport, Acknowledgement>, DeleteContractorsReportHandler>();
        services.AddScoped<DeleteContractorsReportAuthorisation>();
    }

    // The DrawingsStorage fallback matches every other blob feature: prod configures only
    // DrawingsStorage:ConnectionString, and on SWA managed functions AzureWebJobsStorage is the
    // platform's own account, not ours — without this line every progress photo (and the site
    // photo pool) landed on the null store (2026-09-16, Jeremy's first drop: "29 failed").
    // A setting that exists but is BLANK is "unset", not "configured as nothing" — `??` would
    // stop at an empty value and never reach the fallback, so the chain is read as a list.
    private static void RegisterPhotoStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = FirstConfigured(configuration,
            "ProgressPhotosStorage:ConnectionString", "DrawingsStorage:ConnectionString", "AzureWebJobsStorage");

        if (connectionString is null)
            services.AddSingleton<IProgressPhotoStore, NullProgressPhotoStore>();
        else
            services.AddSingleton<IProgressPhotoStore>(_ => new AzureBlobProgressPhotoStore(connectionString));
    }

    private static string? FirstConfigured(IConfiguration configuration, params string[] keys) =>
        keys.Select(key => configuration[key]).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
