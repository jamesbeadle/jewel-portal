using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Manual.Baseline;

namespace Jewel.JPMS.Api.Features.Manual.Commands;

/// <summary>
/// Loads the JBB Site Manager Manual v0.13 (May 2026, a working draft) as DRAFT modules, one per
/// module of the modular restructure. A code already present is left alone, so the load is safe to
/// repeat and never overwrites the office's own work. Owners and approvers are for the office to name.
/// </summary>
public sealed class ImportManualBaselineHandler : ICommandHandler<ImportManualBaseline, ManualBaselineImport>
{
    private readonly JpmsContext context;
    private readonly ManualModuleLoader modules;
    public ImportManualBaselineHandler(JpmsContext context, ManualModuleLoader modules) { this.context = context; this.modules = modules; }

    public async Task<ManualBaselineImport> HandleAsync(ImportManualBaseline command, CancellationToken cancellationToken)
    {
        var present = await context.ManualModules.Select(module => module.Code).ToListAsync(cancellationToken);
        var sequence = await modules.NextSequenceAsync(cancellationToken);
        var created = new List<string>();
        var skipped = new List<string>();
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ManualBaselineCatalogue.Read())
        {
            if (present.Contains(entry.Code)) { skipped.Add(entry.Code); continue; }
            context.ManualModules.Add(NewDraft(entry, sequence, command.ImportedByEmail, now));
            sequence += 1;
            created.Add(entry.Code);
        }
        await context.SaveChangesAsync(cancellationToken);
        return new ManualBaselineImport(created, skipped);
    }

    private static ManualModuleEntity NewDraft(ManualBaselineEntry entry, int sequence, string importedByEmail, DateTimeOffset now)
    {
        var entity = new ManualModuleEntity
        {
            ManualModuleId = ManualIdentifierFactory.NextModuleId(),
            Code = entry.Code,
            Title = entry.Title,
            Purpose = entry.Purpose,
            Body = entry.Body,
            Status = (int)ManualModuleStatus.Draft,
            LinkedFormSlugs = ManualMapping.JoinSlugs(entry.LinkedFormSlugs),
            LinkedStandards = entry.LinkedStandards,
            SourceSections = entry.SourceSections,
            ChangeSummary = ManualBaselineCatalogue.ChangeSummary,
            Sequence = sequence,
            CreatedByEmail = importedByEmail,
            CreatedAt = now,
            UpdatedByEmail = importedByEmail,
            UpdatedAt = now,
        };
        entity.SetAudience(entry.Audience);
        return entity;
    }
}
