
namespace Jewel.JPMS.Api.Features.Manual.Commands;

public sealed class SubmitManualModuleForReviewHandler : ICommandHandler<SubmitManualModuleForReview, ManualModule>
{
    private readonly JpmsContext context;
    private readonly ManualModuleLoader modules;
    public SubmitManualModuleForReviewHandler(JpmsContext context, ManualModuleLoader modules) { this.context = context; this.modules = modules; }

    public async Task<ManualModule> HandleAsync(SubmitManualModuleForReview command, CancellationToken cancellationToken)
    {
        var module = await modules.RequireAsync(command.ManualModuleId, cancellationToken);
        var isDraft = module.Status == (int)ManualModuleStatus.Draft;
        if (!isDraft) throw new InvalidOperationException("Only a draft can be sent for review.");
        if (string.IsNullOrWhiteSpace(module.Body)) throw new InvalidOperationException("The module has no text yet.");
        if (string.IsNullOrEmpty(module.OwnerEmail)) throw new InvalidOperationException("Name the module's owner before sending it for review.");
        if (string.IsNullOrEmpty(module.ApproverEmail)) throw new InvalidOperationException("Name the module's approver before sending it for review.");

        module.Status = (int)ManualModuleStatus.InReview;
        module.UpdatedByEmail = command.SubmittedByEmail;
        module.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return await modules.ReadAsync(module, cancellationToken);
    }
}
