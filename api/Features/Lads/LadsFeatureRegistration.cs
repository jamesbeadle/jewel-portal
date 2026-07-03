using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Lads.Commands;
using Jewel.JPMS.Api.Features.Lads.Queries;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Lads;

public static class LadsFeatureRegistration
{
    public static IServiceCollection AddLadsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListLadClaimsForProject, IReadOnlyList<LadClaim>>, ListLadClaimsForProjectHandler>();

        services.AddScoped<ICommandHandler<AddLadClaim, LadClaim>, AddLadClaimHandler>();
        services.AddScoped<AddLadClaimAuthorisation>();
        services.AddScoped<AddLadClaimValidation>();

        services.AddScoped<ICommandHandler<UpdateLadClaim, LadClaim>, UpdateLadClaimHandler>();
        services.AddScoped<UpdateLadClaimAuthorisation>();
        services.AddScoped<UpdateLadClaimValidation>();

        return services;
    }
}
