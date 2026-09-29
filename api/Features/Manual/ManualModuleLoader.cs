using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Manual;

/// <summary>Finds the module a command names, and counts what a reading needs beside it.</summary>
public sealed class ManualModuleLoader
{
    private readonly JpmsContext context;
    public ManualModuleLoader(JpmsContext context) { this.context = context; }

    public async Task<ManualModuleEntity> RequireAsync(string manualModuleId, CancellationToken cancellationToken)
    {
        var module = await context.ManualModules.FindAsync(new object[] { manualModuleId }, cancellationToken);
        if (module is null) throw new InvalidOperationException("That module no longer exists.");
        return module;
    }

    public async Task<int> NextSequenceAsync(CancellationToken cancellationToken)
    {
        var sequences = await context.ManualModules.Select(module => module.Sequence).ToListAsync(cancellationToken);
        if (sequences.Count == 0) return 1;
        return sequences.Max() + 1;
    }

    public Task<int> PublishedAcknowledgementCountAsync(ManualModuleEntity module, CancellationToken cancellationToken) =>
        context.ManualAcknowledgements.CountAsync(
            row => row.ManualModuleId == module.ManualModuleId && row.Version == module.PublishedVersion, cancellationToken);

    public async Task<ManualModule> ReadAsync(ManualModuleEntity module, CancellationToken cancellationToken)
    {
        var acknowledgedCount = await PublishedAcknowledgementCountAsync(module, cancellationToken);
        return module.ToModel(acknowledgedCount);
    }
}
