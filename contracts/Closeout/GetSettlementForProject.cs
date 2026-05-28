using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

public sealed record GetSettlementForProject(string ProjectId) : IQuery<SettlementRecord?>;
