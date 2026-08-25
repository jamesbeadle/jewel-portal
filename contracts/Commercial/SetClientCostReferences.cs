using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// Replaces the project's whole cost centre → client reference map in one save: entries with a
// reference are kept (added or updated), entries with a blank reference are removed, and any
// cost centre not listed is removed too. Returns the map as saved.
public sealed record SetClientCostReferences(
    string ProjectId,
    IReadOnlyList<ClientCostReferenceEntry> Entries)
    : ICommand<IReadOnlyList<ClientCostReference>>;
