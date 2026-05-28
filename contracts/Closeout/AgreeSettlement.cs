using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

public sealed record AgreeSettlement(
    string ProjectId,
    decimal FinalContractValue,
    decimal FinalCost,
    decimal FinalMargin,
    bool IsClientSigned) : ICommand<SettlementRecord>;
