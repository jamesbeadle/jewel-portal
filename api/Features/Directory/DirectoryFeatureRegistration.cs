using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Directory.Commands;
using Jewel.JPMS.Api.Features.Directory.Queries;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Directory;

public static class DirectoryFeatureRegistration
{
    public static IServiceCollection AddDirectoryFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListDirectoryUsers, IReadOnlyList<DirectoryUser>>, ListDirectoryUsersHandler>();
        services.AddScoped<IQueryHandler<GetDirectoryUser, DirectoryUser?>, GetDirectoryUserHandler>();

        services.AddScoped<ICommandHandler<UpsertDirectoryUser, DirectoryUser>, UpsertDirectoryUserHandler>();
        services.AddScoped<UpsertDirectoryUserAuthorisation>();
        services.AddScoped<UpsertDirectoryUserValidation>();

        services.AddScoped<ICommandHandler<RemoveDirectoryUser, Acknowledgement>, RemoveDirectoryUserHandler>();
        services.AddScoped<RemoveDirectoryUserAuthorisation>();
        services.AddScoped<RemoveDirectoryUserValidation>();

        return services;
    }
}
