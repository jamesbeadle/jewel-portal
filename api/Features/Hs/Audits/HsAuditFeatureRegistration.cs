using Jewel.JPMS.Api.Features.Hs.Audits.Commands;
using Jewel.JPMS.Api.Features.Hs.Audits.Queries;
using Jewel.JPMS.Contracts.Hs;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Hs.Audits;

public static class HsAuditFeatureRegistration
{
    public static IServiceCollection AddHsAuditFeature(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateHsAudit, HsAudit>, CreateHsAuditHandler>();
        services.AddScoped<CreateHsAuditAuthorisation>();
        services.AddScoped<CreateHsAuditValidation>();

        services.AddScoped<ICommandHandler<UpdateHsAuditDetails, HsAudit>, UpdateHsAuditDetailsHandler>();
        services.AddScoped<UpdateHsAuditDetailsAuthorisation>();
        services.AddScoped<UpdateHsAuditDetailsValidation>();

        services.AddScoped<ICommandHandler<UpdateHsAuditItems, HsAuditView>, UpdateHsAuditItemsHandler>();
        services.AddScoped<UpdateHsAuditItemsAuthorisation>();
        services.AddScoped<UpdateHsAuditItemsValidation>();

        services.AddScoped<ICommandHandler<IssueHsAudit, HsAuditView>, IssueHsAuditHandler>();
        services.AddScoped<IssueHsAuditAuthorisation>();

        services.AddScoped<ICommandHandler<CloseHsAudit, HsAudit>, CloseHsAuditHandler>();
        services.AddScoped<CloseHsAuditAuthorisation>();
        services.AddScoped<CloseHsAuditValidation>();

        services.AddScoped<IQueryHandler<ListHsAuditsForProject, IReadOnlyList<HsAudit>>, ListHsAuditsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetHsAudit, HsAuditView>, GetHsAuditHandler>();

        return services;
    }
}
