using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Site.Queries;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Site;

public static class SiteFeatureRegistration
{
    public static IServiceCollection AddSiteFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListSiteReportsForProject, IReadOnlyList<SiteReport>>, ListSiteReportsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetProgrammeForProject, IReadOnlyList<ProgrammeTask>>, GetProgrammeForProjectHandler>();
        services.AddScoped<IQueryHandler<GetProgrammeDetail, ProgrammeDetail>, GetProgrammeDetailHandler>();

        services.AddScoped<ICommandHandler<AssembleSiteReport, SiteReport>, AssembleSiteReportHandler>();
        services.AddScoped<AssembleSiteReportAuthorisation>();
        services.AddScoped<AssembleSiteReportValidation>();

        services.AddScoped<ICommandHandler<ApproveSiteReport, SiteReport>, ApproveSiteReportHandler>();
        services.AddScoped<ApproveSiteReportAuthorisation>();
        services.AddScoped<ApproveSiteReportValidation>();

        services.AddScoped<ICommandHandler<AddProgrammeTask, ProgrammeTask>, AddProgrammeTaskHandler>();
        services.AddScoped<AddProgrammeTaskAuthorisation>();
        services.AddScoped<AddProgrammeTaskValidation>();

        services.AddScoped<ICommandHandler<UpdateProgrammeTask, ProgrammeTask>, UpdateProgrammeTaskHandler>();
        services.AddScoped<UpdateProgrammeTaskAuthorisation>();
        services.AddScoped<UpdateProgrammeTaskValidation>();

        services.AddScoped<ICommandHandler<AddProgrammeTaskLink, ProgrammeTaskLink>, AddProgrammeTaskLinkHandler>();
        services.AddScoped<AddProgrammeTaskLinkAuthorisation>();
        services.AddScoped<AddProgrammeTaskLinkValidation>();

        services.AddScoped<ICommandHandler<RemoveProgrammeTaskLink, Jewel.JPMS.Contracts.Cqrs.Acknowledgement>, RemoveProgrammeTaskLinkHandler>();
        services.AddScoped<RemoveProgrammeTaskLinkAuthorisation>();
        services.AddScoped<RemoveProgrammeTaskLinkValidation>();

        services.AddScoped<ICommandHandler<TakeProgrammeBaseline, ProgrammeBaseline>, TakeProgrammeBaselineHandler>();
        services.AddScoped<TakeProgrammeBaselineAuthorisation>();
        services.AddScoped<TakeProgrammeBaselineValidation>();

        return services;
    }
}
