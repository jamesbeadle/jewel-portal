using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents;

// Resolves agents by key and lists their descriptors. Registered as a singleton; each IRequestAgent
// is registered as a singleton and injected here, so adding an agent is a one-line DI registration.
public sealed class AgentRegistry
{
    private readonly IReadOnlyDictionary<string, IRequestAgent> byKey;

    public AgentRegistry(IEnumerable<IRequestAgent> agents)
    {
        byKey = agents.ToDictionary(a => a.Key, StringComparer.OrdinalIgnoreCase);
    }

    public bool Exists(string key) => byKey.ContainsKey(key);

    public IRequestAgent? Find(string key) => byKey.TryGetValue(key, out var agent) ? agent : null;

    // The predefined agents for a record type — the heart of type-derived applicability. A record of
    // this type has exactly these agents available, with no assignment step. Ordered by discipline for
    // stable display.
    public IReadOnlyList<IRequestAgent> ForRecordType(RecordType type) =>
        byKey.Values
            .Where(a => a.AppliesTo.Contains(type))
            .OrderBy(a => (int)a.Discipline)
            .ToList()
            .AsReadOnly();

    public IReadOnlyList<AgentDescriptor> Descriptors() =>
        byKey.Values
            .OrderBy(a => (int)a.Discipline)
            .Select(a => a.Describe())
            .ToList()
            .AsReadOnly();
}
