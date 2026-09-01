using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Inventory.Commands;
using Jewel.JPMS.Api.Features.Inventory.Queries;
using Jewel.JPMS.Contracts.Inventory;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Inventory;

public static class InventoryFeatureRegistration
{
    public static IServiceCollection AddInventoryFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListInventoryForProject, IReadOnlyList<InventoryItem>>, ListInventoryForProjectHandler>();

        services.AddScoped<ICommandHandler<AddInventoryItem, InventoryItem>, AddInventoryItemHandler>();
        services.AddScoped<AddInventoryItemAuthorisation>();
        services.AddScoped<AddInventoryItemValidation>();

        services.AddScoped<ICommandHandler<UpdateInventoryItem, InventoryItem>, UpdateInventoryItemHandler>();
        services.AddScoped<UpdateInventoryItemAuthorisation>();
        services.AddScoped<UpdateInventoryItemValidation>();

        // The Control Centre's Supplier-pathway "create new → Inventory item": add + link the
        // originating email.
        services.AddScoped<ICommandHandler<CreateInventoryItemFromMessage, InventoryItem>, CreateInventoryItemFromMessageHandler>();
        services.AddScoped<CreateInventoryItemFromMessageAuthorisation>();
        services.AddScoped<CreateInventoryItemFromMessageValidation>();

        return services;
    }
}
