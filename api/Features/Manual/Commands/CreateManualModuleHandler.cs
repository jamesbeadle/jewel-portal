using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class CreateManualModuleHandler : ICommandHandler<CreateManualModule, ManualModule>
{
    private readonly JpmsContext context;
    private readonly ManualModuleLoader modules;
    public CreateManualModuleHandler(JpmsContext context, ManualModuleLoader modules) { this.context = context; this.modules = modules; }

    public async Task<ManualModule> HandleAsync(CreateManualModule command, CancellationToken cancellationToken)
    {
        var code = command.Code.Trim().ToUpperInvariant();
        var isTaken = await context.ManualModules.AnyAsync(module => module.Code == code, cancellationToken);
        if (isTaken) throw new InvalidOperationException($"A module with the code {code} already exists.");

        var entity = NewDraft(command, code, await modules.NextSequenceAsync(cancellationToken));
        context.ManualModules.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(0);
    }

    private static ManualModuleEntity NewDraft(CreateManualModule command, string code, int sequence)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new ManualModuleEntity
        {
            ManualModuleId = ManualIdentifierFactory.NextModuleId(),
            Code = code,
            Title = command.Title.Trim(),
            Purpose = command.Purpose.Trim(),
            Body = command.Body.Trim(),
            OwnerEmail = ManualMapping.NormaliseEmail(command.OwnerEmail),
            ApproverEmail = ManualMapping.NormaliseEmail(command.ApproverEmail),
            Status = (int)ManualModuleStatus.Draft,
            LinkedFormSlugs = ManualMapping.JoinSlugs(command.LinkedFormSlugs),
            LinkedStandards = command.LinkedStandards.Trim(),
            SourceSections = command.SourceSections.Trim(),
            NextReviewAt = command.NextReviewAt,
            Sequence = sequence,
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = now,
            UpdatedByEmail = command.CreatedByEmail,
            UpdatedAt = now,
        };
        entity.SetAudience(command.Audience);
        return entity;
    }
}
