using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Cvr;

public sealed record RecordQsAccrual(
    string ProjectId,
    string Category,
    string Description,
    decimal AddAmount,
    decimal OmitAmount,
    decimal LiabilityAmount,
    string SignedOffByEmail) : ICommand<QsAccrual>;
