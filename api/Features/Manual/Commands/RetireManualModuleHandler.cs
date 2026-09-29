
namespace Jewel.JPMS.Api.Features.Manual.Commands;

/// <summary>A retired module leaves every site view; its versions and acknowledgements stay as the record.</summary>
public sealed class RetireManualModuleHandler : ICommandHandler<RetireManualModule, ManualModule>
{
    private readonly JpmsContext context;
    private readonly ManualModuleLoader modules;
    public RetireManualModuleHandler(JpmsContext context, ManualModuleLoader modules) { this.context = context; this.modules = modules; }

    public async Task<ManualModule> HandleAsync(RetireManualModule command, CancellationToken cancellationToken)
    {
        var module = await modules.RequireAsync(command.ManualModuleId, cancellationToken);
        var isRetired = module.Status == (int)ManualModuleStatus.Superseded;
        if (isRetired) throw new InvalidOperationException("That module is already retired.");

        var now = DateTimeOffset.UtcNow;
        var open = await context.ManualModuleVersions
            .Where(version => version.ManualModuleId == module.ManualModuleId && version.SupersededAt == null)
            .ToListAsync(cancellationToken);
        foreach (var version in open) version.SupersededAt = now;
        module.Status = (int)ManualModuleStatus.Superseded;
        module.UpdatedByEmail = command.RetiredByEmail;
        module.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);
        return await modules.ReadAsync(module, cancellationToken);
    }
}
