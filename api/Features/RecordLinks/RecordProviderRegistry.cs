using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// Resolves the ILinkableRecordProvider for a record type. Mirrors AgentRegistry.ForRecordType: the
// set of providers is fixed by what's registered in DI, and applicability is derived from each
// provider's Type — never assigned. Adding a record type adds a provider; nothing here changes.
public sealed class RecordProviderRegistry
{
    private readonly IReadOnlyDictionary<RecordType, ILinkableRecordProvider> byType;

    public RecordProviderRegistry(IEnumerable<ILinkableRecordProvider> providers)
    {
        var map = new Dictionary<RecordType, ILinkableRecordProvider>();
        foreach (var provider in providers)
        {
            if (map.ContainsKey(provider.Type))
                throw new InvalidOperationException(
                    $"Two linkable-record providers registered for {provider.Type}.");
            map[provider.Type] = provider;
        }
        byType = map;
    }

    public ILinkableRecordProvider For(RecordType type) =>
        byType.TryGetValue(type, out var provider)
            ? provider
            : throw new InvalidOperationException($"No linkable-record provider for record type {type}.");

    public bool TryGet(RecordType type, out ILinkableRecordProvider provider) =>
        byType.TryGetValue(type, out provider!);

    public IEnumerable<ILinkableRecordProvider> All => byType.Values;
}
