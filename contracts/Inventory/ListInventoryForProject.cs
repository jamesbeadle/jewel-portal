using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Inventory;

public sealed record ListInventoryForProject(string ProjectId) : IQuery<IReadOnlyList<InventoryItem>>;
