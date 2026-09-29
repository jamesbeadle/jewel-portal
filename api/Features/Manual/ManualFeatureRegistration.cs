using Jewel.JPMS.Api.Features.Manual.Commands;
using Jewel.JPMS.Api.Features.Manual.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Manual;

/// <summary>
/// The site manual as controlled modules (2026-09-29): the office master, the role views, the
/// draft → in review → approved cycle with every approved version kept, acknowledgements against
/// the version read, and the JBB baseline loaded as drafts.
/// </summary>
public static class ManualFeatureRegistration
{
    public static IServiceCollection AddManualFeature(this IServiceCollection services)
    {
        services.AddScoped<ManualAuthorisationGate>();
        services.AddScoped<ManualValidationGate>();
        services.AddScoped<ManualModuleLoader>();
        services.AddScoped<ManualCommandGate>();
        services.AddScoped<IQueryHandler<ListManualModules, IReadOnlyList<ManualModule>>, ListManualModulesHandler>();
        services.AddScoped<IQueryHandler<GetManualModule, ManualModuleDetail?>, GetManualModuleHandler>();
        services.AddScoped<GetManualViewHandler>();
        services.AddScoped<IQueryHandler<GetManualView, ManualViewReading>>(provider => provider.GetRequiredService<GetManualViewHandler>());
        services.AddScoped<ICommandHandler<CreateManualModule, ManualModule>, CreateManualModuleHandler>();
        services.AddScoped<ICommandHandler<UpdateManualModuleDraft, ManualModule>, UpdateManualModuleDraftHandler>();
        services.AddScoped<ICommandHandler<SubmitManualModuleForReview, ManualModule>, SubmitManualModuleForReviewHandler>();
        services.AddScoped<ICommandHandler<ApproveManualModule, ManualModule>, ApproveManualModuleHandler>();
        services.AddScoped<ICommandHandler<ReturnManualModuleToDraft, ManualModule>, ReturnManualModuleToDraftHandler>();
        services.AddScoped<ICommandHandler<ReviseManualModule, ManualModule>, ReviseManualModuleHandler>();
        services.AddScoped<ICommandHandler<RetireManualModule, ManualModule>, RetireManualModuleHandler>();
        services.AddScoped<ICommandHandler<AcknowledgeManualModule, ManualAcknowledgement>, AcknowledgeManualModuleHandler>();
        services.AddScoped<ICommandHandler<ImportManualBaseline, ManualBaselineImport>, ImportManualBaselineHandler>();
        return services;
    }
}
