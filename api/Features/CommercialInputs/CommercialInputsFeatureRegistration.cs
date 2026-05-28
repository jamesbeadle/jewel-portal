using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.CommercialInputs.Commands;
using Jewel.JPMS.Api.Features.CommercialInputs.Queries;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.CommercialInputs;

public static class CommercialInputsFeatureRegistration
{
    public static IServiceCollection AddCommercialInputsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListDayworksForProject, IReadOnlyList<Daywork>>, ListDayworksForProjectHandler>();
        services.AddScoped<IQueryHandler<ListContraChargesForProject, IReadOnlyList<ContraCharge>>, ListContraChargesForProjectHandler>();
        services.AddScoped<IQueryHandler<ListSubcontractorRetentionsForProject, IReadOnlyList<SubcontractorRetention>>, ListSubcontractorRetentionsForProjectHandler>();

        services.AddScoped<ICommandHandler<LogDaywork, Daywork>, LogDayworkHandler>();
        services.AddScoped<LogDayworkAuthorisation>();
        services.AddScoped<LogDayworkValidation>();

        services.AddScoped<ICommandHandler<RecordContraCharge, ContraCharge>, RecordContraChargeHandler>();
        services.AddScoped<RecordContraChargeAuthorisation>();
        services.AddScoped<RecordContraChargeValidation>();

        services.AddScoped<ICommandHandler<RecordSubcontractorRetention, SubcontractorRetention>, RecordSubcontractorRetentionHandler>();
        services.AddScoped<RecordSubcontractorRetentionAuthorisation>();
        services.AddScoped<RecordSubcontractorRetentionValidation>();

        return services;
    }
}
