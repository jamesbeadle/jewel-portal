using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Directory;

public static class DirectoryRouteRegistration
{
    public static IServiceCollection AddDirectoryReadModels(this IServiceCollection services)
    {
        services.AddScoped<DirectoryReadModel>();
        services.AddScoped<RevokedDirectoryReadModel>();
        services.AddScoped<AccessRequestsReadModel>();
        return services;
    }

    public static void RegisterDirectoryRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListDirectoryUsers, IReadOnlyList<DirectoryUser>>(QueryRoute.Static("/api/directory"));
        queries.Register<ListRevokedDirectoryUsers, IReadOnlyList<RevokedDirectoryUser>>(QueryRoute.Static("/api/directory/revoked"));
        queries.Register<GetDirectoryUser, DirectoryUser?>(new QueryRoute(
            "/api/directory/{email}",
            query => $"/api/directory/{Uri.EscapeDataString(((GetDirectoryUser)query).Email)}"));
        queries.Register<ListPendingAccessRequests, IReadOnlyList<AccessRequest>>(QueryRoute.Static("/api/access-requests"));

        commands.Register<UpsertDirectoryUser, DirectoryUser>(CommandRoute.Post("/api/directory"));
        commands.Register<RemoveDirectoryUser, Acknowledgement>(new CommandRoute(
            "DELETE",
            "/api/directory/{email}",
            command => $"/api/directory/{Uri.EscapeDataString(((RemoveDirectoryUser)command).Email)}"));
        commands.Register<RestoreDirectoryUser, Acknowledgement>(new CommandRoute(
            "POST",
            "/api/directory/{email}/restore",
            command => $"/api/directory/{Uri.EscapeDataString(((RestoreDirectoryUser)command).Email)}/restore"));
        commands.Register<DeleteDirectoryUser, Acknowledgement>(new CommandRoute(
            "DELETE",
            "/api/directory/{email}/permanent",
            command => $"/api/directory/{Uri.EscapeDataString(((DeleteDirectoryUser)command).Email)}/permanent"));
        commands.Register<SubmitAccessRequest, AccessRequest>(CommandRoute.Post("/api/access-requests"));
        commands.Register<ResolveAccessRequest, Acknowledgement>(new CommandRoute(
            "POST",
            "/api/access-requests/{email}/resolve",
            command => $"/api/access-requests/{Uri.EscapeDataString(((ResolveAccessRequest)command).Email)}/resolve"));
    }
}
