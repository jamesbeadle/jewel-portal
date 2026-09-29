using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

/// <summary>Drops the rewrite, so Section 1 prints the raw notes again and no flag gates the build.</summary>
public sealed class DiscardContractorsReportRewriteHandler : ICommandHandler<DiscardContractorsReportRewrite, ContractorsReport>
{
    private readonly JpmsContext context;
    public DiscardContractorsReportRewriteHandler(JpmsContext context) { this.context = context; }

    public async Task<ContractorsReport> HandleAsync(DiscardContractorsReportRewrite command, CancellationToken cancellationToken)
    {
        var entity = await context.ContractorsReports.FirstOrDefaultAsync(row => row.ContractorsReportId == command.ContractorsReportId, cancellationToken)
            ?? throw new InvalidOperationException("Contractor's Report not found.");
        entity.RewriteJson = null;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
