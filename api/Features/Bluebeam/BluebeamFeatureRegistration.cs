using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Bluebeam.Extraction;
using Jewel.JPMS.Api.Features.Bluebeam.Queue;
using Jewel.JPMS.Contracts.Drawings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Bluebeam;

public static class BluebeamFeatureRegistration
{
    public static IServiceCollection AddBluebeamFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = BluebeamOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            // Own HttpClient instance, like the Anthropic and Places clients.
            services.AddSingleton<IBluebeamClient>(sp =>
                new BluebeamClient(new HttpClient(), options, sp.GetRequiredService<ILogger<BluebeamClient>>()));
        }
        else
        {
            services.AddSingleton<IBluebeamClient, NullBluebeamClient>();
        }

        // Scoped — they write through the request's JpmsContext.
        services.AddScoped<BluebeamTokenService>();
        services.AddScoped<BluebeamConnectionWriter>();

        RegisterQueue(services, configuration);

        services.AddScoped<ICommandHandler<QueueDrawingExtraction, DrawingExtraction>, QueueDrawingExtractionHandler>();
        services.AddScoped<QueueDrawingExtractionAuthorisation>();
        services.AddScoped<QueueDrawingExtractionValidation>();

        services.AddScoped<ICommandHandler<QueueProjectDrawingExtractions, int>, QueueProjectDrawingExtractionsHandler>();
        services.AddScoped<QueueProjectDrawingExtractionsAuthorisation>();
        services.AddScoped<QueueProjectDrawingExtractionsValidation>();

        services.AddScoped<IQueryHandler<GetDrawingExtraction, DrawingExtractionView?>, GetDrawingExtractionHandler>();

        return services;
    }

    private static void RegisterQueue(IServiceCollection services, IConfiguration configuration)
    {
        // Same account resolution as the mailbox queues — both apps must see the same queues.
        var connectionString = configuration["MailboxQueuesConnection"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IDrawingExtractionQueue, NullDrawingExtractionQueue>();
        else
            services.AddSingleton<IDrawingExtractionQueue>(_ => new StorageDrawingExtractionQueue(connectionString));
    }
}
