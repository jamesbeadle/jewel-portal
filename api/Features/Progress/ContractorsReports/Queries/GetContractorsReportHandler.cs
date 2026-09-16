using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Queries;

public sealed class GetContractorsReportHandler : IQueryHandler<GetContractorsReport, ContractorsReportView?>
{
    private readonly ContractorsReportComposer composer;
    public GetContractorsReportHandler(ContractorsReportComposer composer) { this.composer = composer; }

    public Task<ContractorsReportView?> HandleAsync(GetContractorsReport query, CancellationToken cancellationToken) =>
        composer.ViewAsync(query.ContractorsReportId, cancellationToken);
}
