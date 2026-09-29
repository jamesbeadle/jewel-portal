
namespace Jewel.JPMS.Api.Features.Manual.Queries;

/// <summary>The office master: every module in family order, each with how many have read its published version.</summary>
public sealed class ListManualModulesHandler : IQueryHandler<ListManualModules, IReadOnlyList<ManualModule>>
{
    private readonly JpmsContext context;
    public ListManualModulesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ManualModule>> HandleAsync(ListManualModules query, CancellationToken cancellationToken)
    {
        var modules = await context.ManualModules.ToListAsync(cancellationToken);
        var counts = await ManualAcknowledgementCounts.ForPublishedVersionsAsync(context, cancellationToken);
        return modules
            .OrderBy(module => ManualModuleCodes.FamilyOrder(ManualModuleCodes.FamilyOf(module.Code)))
            .ThenBy(module => module.Code)
            .Select(module => module.ToModel(counts.Of(module.ManualModuleId, module.PublishedVersion)))
            .ToList();
    }
}
