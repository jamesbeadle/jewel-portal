using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.ProjectContracts.Commands;
using Jewel.JPMS.Api.Features.ProjectContracts.Queries;
using Jewel.JPMS.Api.Features.ProjectContracts.Storage;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.ProjectContracts;

public static class ProjectContractsFeatureRegistration
{
    public static IServiceCollection AddProjectContractsFeature(
        this IServiceCollection services, IConfiguration configuration)
    {
        RegisterBlobStore(services, configuration);

        services.AddScoped<IQueryHandler<GetProjectContract, ProjectContract?>, GetProjectContractHandler>();
        services.AddScoped<IQueryHandler<ListProjectContractAmendments, IReadOnlyList<ProjectContractAmendment>>, ListProjectContractAmendmentsHandler>();

        services.AddScoped<ICommandHandler<SetProjectContractTerms, ProjectContract>, SetProjectContractTermsHandler>();
        services.AddScoped<SetProjectContractTermsAuthorisation>();
        services.AddScoped<SetProjectContractTermsValidation>();

        services.AddScoped<ICommandHandler<AttachProjectContractDocument, ProjectContract>, AttachProjectContractDocumentHandler>();
        services.AddScoped<AttachProjectContractDocumentAuthorisation>();
        services.AddScoped<AttachProjectContractDocumentValidation>();

        services.AddScoped<ICommandHandler<AttachProjectContractAmendment, ProjectContractAmendment>, AttachProjectContractAmendmentHandler>();
        services.AddScoped<AttachProjectContractAmendmentAuthorisation>();
        services.AddScoped<AttachProjectContractAmendmentValidation>();

        services.AddScoped<ICommandHandler<SetProjectContractAmendmentDetails, ProjectContractAmendment>, SetProjectContractAmendmentDetailsHandler>();
        services.AddScoped<SetProjectContractAmendmentDetailsAuthorisation>();
        services.AddScoped<SetProjectContractAmendmentDetailsValidation>();

        services.AddScoped<ICommandHandler<RemoveProjectContractAmendment, Acknowledgement>, RemoveProjectContractAmendmentHandler>();
        services.AddScoped<RemoveProjectContractAmendmentAuthorisation>();
        services.AddScoped<RemoveProjectContractAmendmentValidation>();

        return services;
    }

    private static void RegisterBlobStore(IServiceCollection services, IConfiguration configuration)
    {
        // Falls back through the drawings account before the platform default, so a deployment that
        // has already configured document storage needs no new setting.
        var connectionString = configuration["ProjectContractsStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IProjectContractBlobStore, NullProjectContractBlobStore>();
        else
            services.AddSingleton<IProjectContractBlobStore>(_ => new AzureBlobProjectContractStore(connectionString));
    }
}
