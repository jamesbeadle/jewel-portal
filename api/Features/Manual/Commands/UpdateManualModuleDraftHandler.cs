
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class UpdateManualModuleDraftHandler : ICommandHandler<UpdateManualModuleDraft, ManualModule>
{
    private readonly JpmsContext context;
    private readonly ManualModuleLoader modules;
    public UpdateManualModuleDraftHandler(JpmsContext context, ManualModuleLoader modules) { this.context = context; this.modules = modules; }

    public async Task<ManualModule> HandleAsync(UpdateManualModuleDraft command, CancellationToken cancellationToken)
    {
        var module = await modules.RequireAsync(command.ManualModuleId, cancellationToken);
        var isDraft = module.Status == (int)ManualModuleStatus.Draft;
        if (!isDraft) throw new InvalidOperationException("Only a draft can be edited — return it to draft first.");

        module.Title = command.Title.Trim();
        module.Purpose = command.Purpose.Trim();
        module.Body = command.Body.Trim();
        module.OwnerEmail = ManualMapping.NormaliseEmail(command.OwnerEmail);
        module.ApproverEmail = ManualMapping.NormaliseEmail(command.ApproverEmail);
        module.LinkedFormSlugs = ManualMapping.JoinSlugs(command.LinkedFormSlugs);
        module.LinkedStandards = command.LinkedStandards.Trim();
        module.ChangeSummary = command.ChangeSummary.Trim();
        module.NextReviewAt = command.NextReviewAt;
        module.SetAudience(command.Audience);
        module.UpdatedByEmail = command.UpdatedByEmail;
        module.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return await modules.ReadAsync(module, cancellationToken);
    }
}
