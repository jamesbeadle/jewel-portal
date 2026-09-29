using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Manual.Commands;

/// <summary>The reader's own acknowledgement of the published version — once per version, the typed name the record.</summary>
public sealed class AcknowledgeManualModuleHandler : ICommandHandler<AcknowledgeManualModule, ManualAcknowledgement>
{
    private readonly JpmsContext context;
    private readonly ManualModuleLoader modules;
    public AcknowledgeManualModuleHandler(JpmsContext context, ManualModuleLoader modules) { this.context = context; this.modules = modules; }

    public async Task<ManualAcknowledgement> HandleAsync(AcknowledgeManualModule command, CancellationToken cancellationToken)
    {
        var module = await modules.RequireAsync(command.ManualModuleId, cancellationToken);
        var isPublished = module.PublishedVersion > 0 && module.Status != (int)ManualModuleStatus.Superseded;
        if (!isPublished) throw new InvalidOperationException("That module has no approved version to acknowledge.");

        var email = ManualMapping.NormaliseEmail(command.AcknowledgedByEmail);
        var existing = await context.ManualAcknowledgements.FirstOrDefaultAsync(
            row => row.ManualModuleId == module.ManualModuleId && row.Version == module.PublishedVersion && row.Email == email,
            cancellationToken);
        if (existing is not null) return existing.ToModel();

        var acknowledgement = new ManualAcknowledgementEntity
        {
            ManualAcknowledgementId = ManualIdentifierFactory.NextAcknowledgementId(),
            ManualModuleId = module.ManualModuleId,
            Version = module.PublishedVersion,
            Email = email,
            TypedName = command.TypedName.Trim(),
            AcknowledgedAt = DateTimeOffset.UtcNow,
        };
        context.ManualAcknowledgements.Add(acknowledgement);
        await context.SaveChangesAsync(cancellationToken);
        return acknowledgement.ToModel();
    }
}
