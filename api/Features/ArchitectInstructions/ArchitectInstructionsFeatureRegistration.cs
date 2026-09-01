using Jewel.JPMS.Api.Features.ArchitectInstructions.Storage;
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.ArchitectInstructions;

public static class ArchitectInstructionsFeatureRegistration
{
    public static IServiceCollection AddArchitectInstructionsFeature(
        this IServiceCollection services, IConfiguration configuration)
    {
        RegisterBlobStore(services, configuration);

        services.AddScoped<IQueryHandler<ListArchitectInstructionsForProject, IReadOnlyList<ArchitectInstruction>>,
            ListArchitectInstructionsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetArchitectInstructionById, ArchitectInstruction?>,
            GetArchitectInstructionByIdHandler>();

        services.AddScoped<ICommandHandler<RecordArchitectInstruction, ArchitectInstruction>,
            RecordArchitectInstructionHandler>();
        services.AddScoped<ICommandHandler<ImportArchitectInstructionFromMessage, ArchitectInstruction>,
            ImportArchitectInstructionFromMessageHandler>();
        services.AddScoped<ICommandHandler<UpdateArchitectInstruction, ArchitectInstruction>,
            UpdateArchitectInstructionHandler>();
        services.AddScoped<ICommandHandler<LinkArchitectInstructionToVariation, ArchitectInstruction>,
            LinkArchitectInstructionToVariationHandler>();
        services.AddScoped<ICommandHandler<UnlinkArchitectInstructionFromVariation, ArchitectInstruction>,
            UnlinkArchitectInstructionFromVariationHandler>();
        services.AddScoped<ICommandHandler<DeleteArchitectInstruction, Acknowledgement>,
            DeleteArchitectInstructionHandler>();

        // The gate classes the connector's action gateway composes (2026-08-31).
        services.AddScoped<ImportArchitectInstructionFromMessageAuthorisation>();
        services.AddScoped<UpdateArchitectInstructionAuthorisation>();
        services.AddScoped<LinkArchitectInstructionToVariationAuthorisation>();
        services.AddScoped<UnlinkArchitectInstructionFromVariationAuthorisation>();
        services.AddScoped<DeleteArchitectInstructionAuthorisation>();

        return services;
    }

    // Instruction documents share the drawings storage account by default — same lifetime, same
    // backup story, one connection string to configure — but can be pointed elsewhere if the
    // contract archive ever needs its own account.
    private static void RegisterBlobStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ArchitectInstructionsStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IArchitectInstructionBlobStore, NullArchitectInstructionBlobStore>();
        else
            services.AddSingleton<IArchitectInstructionBlobStore>(
                _ => new AzureBlobArchitectInstructionStore(connectionString));
    }
}
