using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CashCalls;

public sealed record ListCashCallsForProject(string ProjectId) : IQuery<IReadOnlyList<CashCall>>;
