using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

public sealed class DeleteContractorsReportHandler : ICommandHandler<DeleteContractorsReport, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteContractorsReportHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteContractorsReport command, CancellationToken cancellationToken)
    {
        var entity = await context.ContractorsReports.FirstOrDefaultAsync(row => row.ContractorsReportId == command.ContractorsReportId, cancellationToken)
            ?? throw new InvalidOperationException("Contractor's Report not found.");
        context.ContractorsReports.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(entity.ContractorsReportId);
    }
}
