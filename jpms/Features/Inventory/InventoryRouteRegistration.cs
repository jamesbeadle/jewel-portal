using Jewel.JPMS.Contracts.Inventory;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Inventory;

public static class InventoryRouteRegistration
{
    public static IServiceCollection AddInventoryReadModels(this IServiceCollection services)
    {
        services.AddScoped<InventoryReadModel>();
        return services;
    }

    public static void RegisterInventoryRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListInventoryForProject, IReadOnlyList<InventoryItem>>(
            new QueryRoute("/api/projects/{projectId}/inventory",
                query => $"/api/projects/{((ListInventoryForProject)query).ProjectId}/inventory"));

        commands.Register<AddInventoryItem, InventoryItem>(
            new CommandRoute("POST", "/api/projects/{projectId}/inventory",
                command => $"/api/projects/{((AddInventoryItem)command).ProjectId}/inventory"));

        commands.Register<UpdateInventoryItem, InventoryItem>(
            new CommandRoute("PUT", "/api/inventory/{inventoryItemId}",
                command => $"/api/inventory/{((UpdateInventoryItem)command).InventoryItemId}"));

        // The Control Centre's Supplier-pathway "create new → Inventory item": add + link the
        // originating email.
        commands.Register<CreateInventoryItemFromMessage, InventoryItem>(
            new CommandRoute("POST", "/api/mailbox/message/create-inventory-item",
                _ => "/api/mailbox/message/create-inventory-item"));
    }
}
