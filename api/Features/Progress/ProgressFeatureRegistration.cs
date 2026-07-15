using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Progress.Queries;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
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

        services.AddScoped<ICommandHandler<CreateProgressUpdate, ProgressUpdate>, CreateProgressUpdateHandler>();
        services.AddScoped<CreateProgressUpdateAuthorisation>();
        services.AddScoped<CreateProgressUpdateValidation>();

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

        return services;
    }

    private static void RegisterPhotoStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ProgressPhotosStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IProgressPhotoStore, NullProgressPhotoStore>();
        else
            services.AddSingleton<IProgressPhotoStore>(_ => new AzureBlobProgressPhotoStore(connectionString));
    }
}
