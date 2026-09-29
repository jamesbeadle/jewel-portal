
namespace Jewel.JPMS.Api.Features.Manual.Queries;

/// <summary>One module in full: the master row, the text the site sees, every approved version, every acknowledgement.</summary>
public sealed class GetManualModuleHandler : IQueryHandler<GetManualModule, ManualModuleDetail?>
{
    private readonly JpmsContext context;
    public GetManualModuleHandler(JpmsContext context) { this.context = context; }

    public async Task<ManualModuleDetail?> HandleAsync(GetManualModule query, CancellationToken cancellationToken)
    {
        var module = await context.ManualModules.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ManualModuleId == query.ManualModuleId, cancellationToken);
        if (module is null) return null;

        var counts = await ManualAcknowledgementCounts.ForModuleAsync(context, module.ManualModuleId, cancellationToken);
        var versions = await context.ManualModuleVersions.AsNoTracking()
            .Where(version => version.ManualModuleId == module.ManualModuleId)
            .OrderByDescending(version => version.Version)
            .ToListAsync(cancellationToken);
        var acknowledgements = await context.ManualAcknowledgements.AsNoTracking()
            .Where(row => row.ManualModuleId == module.ManualModuleId)
            .OrderByDescending(row => row.Version).ThenBy(row => row.Email)
            .ToListAsync(cancellationToken);
        return new ManualModuleDetail(
            module.ToModel(counts.Of(module.ManualModuleId, module.PublishedVersion)),
            module.PublishedBody,
            versions.Select(version => version.ToModel(counts.Of(module.ManualModuleId, version.Version))).ToList(),
            acknowledgements.Select(row => row.ToModel()).ToList());
    }
}
