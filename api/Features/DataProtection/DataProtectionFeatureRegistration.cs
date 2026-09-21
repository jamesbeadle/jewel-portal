using Jewel.JPMS.Api.Features.DataProtection.Commands;
using Jewel.JPMS.Api.Features.DataProtection.Queries;
using Jewel.JPMS.Contracts.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.DataProtection;

public static class DataProtectionFeatureRegistration
{
    public static IServiceCollection AddDataProtectionFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetPersonDossier, PersonDossier>, GetPersonDossierHandler>();
        services.AddScoped<ICommandHandler<AnonymisePerson, PersonAnonymisation>, AnonymisePersonHandler>();
        services.AddScoped<AnonymisePersonAuthorisation>();
        services.AddScoped<AnonymisePersonValidation>();
        return services;
    }
}
