using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record ListWorkOrders : IQuery<IReadOnlyList<WorkOrder>>;
