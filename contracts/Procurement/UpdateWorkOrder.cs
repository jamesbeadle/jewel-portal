using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record UpdateWorkOrder(
    string WorkOrderId,
    decimal Value,
    string Scope) : ICommand<WorkOrder>;
