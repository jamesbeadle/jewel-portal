namespace Jewel.JPMS.Contracts.Ai;

/// <summary>One model the user can pick in the chat panel. <see cref="Key"/> is what crosses the
/// wire; the actual Anthropic model id it maps to lives server-side in AnthropicOptions, so ids can
/// be repointed by config without touching the client.</summary>
public sealed record AiModelChoice(string Key, string DisplayName, string Hint);

/// <summary>
/// The models the assistant can run on, cheapest first. Shared by the panel (the picker) and the
/// server (key validation) so the two cannot drift.
///
/// <para>The DEFAULT is the cheap one, deliberately: most questions are one look-up and a sentence,
/// and paying Fable rates for "what's the status of V72" is the cost problem this picker exists to
/// solve. The user's own choice is remembered per browser and wins from then on.</para>
/// </summary>
public static class AiModelCatalogue
{
    public const string DefaultKey = "haiku";

    public static readonly IReadOnlyList<AiModelChoice> All = new[]
    {
        new AiModelChoice("haiku", "Haiku", "Fast and cheap — right for most questions"),
        new AiModelChoice("opus", "Opus", "Smarter and slower — for harder commercial reasoning"),
        new AiModelChoice("fable", "Fable", "The strongest and priciest — for the hardest work"),
    };

    public static AiModelChoice? Find(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : All.FirstOrDefault(choice => string.Equals(choice.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>A known key, or the default. Unknown keys degrade to cheap rather than failing —
    /// a stale client must never be what upgrades a call to the expensive model.</summary>
    public static string Normalise(string? key) => Find(key)?.Key ?? DefaultKey;
}
