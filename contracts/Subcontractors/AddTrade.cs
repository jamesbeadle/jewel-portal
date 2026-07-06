using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

// Adds a trade to the curated master list. The name is normalised (trimmed, first letter
// capitalised) and matched case-insensitively — adding an existing trade returns it unchanged.
public sealed record AddTrade(string Name) : ICommand<Trade>;
