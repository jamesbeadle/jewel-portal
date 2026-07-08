using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Every work order on the project, with lines and supplier names — the Work Orders tab's single
// fetch. Ordered by order number; the tab groups the lines by cost code.
public sealed record ListProjectWorkOrders(string ProjectId) : IQuery<IReadOnlyList<ProjectWorkOrderDetail>>;
