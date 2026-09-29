
namespace Jewel.JPMS.Api.Features.Manual.Commands;

/// <summary>Opens the next version as a draft. The approved text stays published until the draft is approved in its turn.</summary>
public sealed class ReviseManualModuleHandler : ICommandHandler<ReviseManualModule, ManualModule>
{
    private readonly JpmsContext context;
    private readonly ManualModuleLoader modules;
    public ReviseManualModuleHandler(JpmsContext context, ManualModuleLoader modules) { this.context = context; this.modules = modules; }

    public async Task<ManualModule> HandleAsync(ReviseManualModule command, CancellationToken cancellationToken)
    {
        var module = await modules.RequireAsync(command.ManualModuleId, cancellationToken);
        var isApproved = module.Status == (int)ManualModuleStatus.Approved;
        if (!isApproved) throw new InvalidOperationException("Only an approved module can be revised — a draft is already open.");

        module.Version = module.PublishedVersion + 1;
        module.Status = (int)ManualModuleStatus.Draft;
        module.ChangeSummary = "";
        module.UpdatedByEmail = command.RevisedByEmail;
        module.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return await modules.ReadAsync(module, cancellationToken);
    }
}
