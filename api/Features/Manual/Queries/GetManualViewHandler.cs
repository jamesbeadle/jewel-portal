
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Manual.Queries;

/// <summary>
/// A published view: only approved, unretired modules whose audience includes the view, showing the
/// published text — never a draft — with the reader's own acknowledgement of that version.
/// </summary>
public sealed class GetManualViewHandler : IQueryHandler<GetManualView, ManualViewReading>
{
    private readonly JpmsContext context;
    public GetManualViewHandler(JpmsContext context) { this.context = context; }

    public Task<ManualViewReading> HandleAsync(GetManualView query, CancellationToken cancellationToken) =>
        HandleAsync(query, readerEmail: "", cancellationToken);

    public async Task<ManualViewReading> HandleAsync(GetManualView query, string readerEmail, CancellationToken cancellationToken)
    {
        var email = ManualMapping.NormaliseEmail(readerEmail);
        var published = await context.ManualModules.AsNoTracking()
            .Where(module => module.PublishedVersion > 0 && module.Status != (int)ManualModuleStatus.Superseded)
            .ToListAsync(cancellationToken);
        var moduleIds = published.Select(module => module.ManualModuleId).ToList();
        var acknowledged = await context.ManualAcknowledgements.AsNoTracking()
            .Where(row => row.Email == email && moduleIds.Contains(row.ManualModuleId))
            .ToListAsync(cancellationToken);
        var modules = published
            .Where(module => module.Audience().Includes(query.View))
            .OrderBy(module => ManualModuleCodes.FamilyOrder(ManualModuleCodes.FamilyOf(module.Code)))
            .ThenBy(module => module.Code)
            .Select(module => module.ToPublishedModel(AcknowledgedAt(acknowledged, module.ManualModuleId, module.PublishedVersion)))
            .ToList();
        return new ManualViewReading(query.View, modules, DateTimeOffset.UtcNow);
    }

    private static DateTimeOffset? AcknowledgedAt(IEnumerable<ManualAcknowledgementEntity> rows, string manualModuleId, int version) =>
        rows.FirstOrDefault(row => row.ManualModuleId == manualModuleId && row.Version == version)?.AcknowledgedAt;
}
