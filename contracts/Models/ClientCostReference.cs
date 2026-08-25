namespace Jewel.JPMS.Models;

// The client's own reference for one of our cost centres on one project — the item number in
// the architect's schedule of works ("3.12", "2.1–2.4") that the client reconciles our valuation
// against. Project-specific: the same cost centre carries a different reference on every job.
// Printed beside the code on the client-facing valuation report PDF; nowhere else.
public sealed record ClientCostReference(
    string ClientCostReferenceId,
    string ProjectId,
    string CostCode,
    string ClientReference);

// One row of the map as the user keys it: the cost centre and the client's reference for it.
// A blank reference removes the mapping for that cost centre.
public sealed record ClientCostReferenceEntry(
    string CostCode,
    string ClientReference);
