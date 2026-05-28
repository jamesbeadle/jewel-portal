using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Rates.Commands;
using Jewel.JPMS.Api.Features.Rates.Queries;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Rates;

public static class RatesFeatureRegistration
{
    public static IServiceCollection AddRatesFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListRatesInLibrary, IReadOnlyList<Rate>>, ListRatesInLibraryHandler>();

        services.AddScoped<ICommandHandler<AddRate, Rate>, AddRateHandler>();
        services.AddScoped<AddRateAuthorisation>();
        services.AddScoped<AddRateValidation>();

        services.AddScoped<ICommandHandler<ReviseRate, Rate>, ReviseRateHandler>();
        services.AddScoped<ReviseRateAuthorisation>();
        services.AddScoped<ReviseRateValidation>();

        return services;
    }
}
