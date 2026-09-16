using System.Text.Json;

namespace Jewel.JPMS.Features.Sales;

/// <summary>What the works view needs to know about a stored definition before the scene is
/// built: the ordered phases, the trades with their colours, and the elements — read from the
/// JSON the estimate stores (the jpms-house-model skill), never from a typed model.</summary>
public sealed record WorksModelDefinition(IReadOnlyList<WorksPhase> Phases, IReadOnlyList<WorksTrade> Trades, int ElementCount)
{
    public static readonly WorksModelDefinition Empty = new(Array.Empty<WorksPhase>(), Array.Empty<WorksTrade>(), 0);

    public bool HasWorks => Phases.Count > 0 && ElementCount > 0;

    public static WorksModelDefinition Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Empty;
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var phases = DefinitionJson.Rows(root, "phases").Select(PhaseOf).ToList();
        var trades = DefinitionJson.Rows(root, "trades").Select(TradeOf).ToList();
        return new WorksModelDefinition(phases, trades, DefinitionJson.Rows(root, "elements").Count());
    }

    private static WorksPhase PhaseOf(JsonElement row) => new(DefinitionJson.Text(row, "key"), DefinitionJson.Text(row, "name"));

    private static WorksTrade TradeOf(JsonElement row) =>
        new(DefinitionJson.Text(row, "key"), DefinitionJson.Text(row, "name"), DefinitionJson.Text(row, "colour"));
}

/// <summary>Reading the definition's JSON without a typed model: an array property as rows, a
/// string property as text, each empty when absent.</summary>
internal static class DefinitionJson
{
    public static IEnumerable<JsonElement> Rows(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var rows)) return Enumerable.Empty<JsonElement>();
        if (rows.ValueKind != JsonValueKind.Array) return Enumerable.Empty<JsonElement>();
        return rows.EnumerateArray();
    }

    public static string Text(JsonElement row, string property)
    {
        if (!row.TryGetProperty(property, out var value)) return "";
        if (value.ValueKind != JsonValueKind.String) return "";
        return value.GetString() ?? "";
    }
}

public sealed record WorksPhase(string Key, string Name);

public sealed record WorksTrade(string Key, string Name, string Colour);

/// <summary>An element as the page shows it when clicked — the fields the viewer hands back.</summary>
public sealed record WorksElement(string Id, string Name, string Trade, string? PhaseIn, string? PhaseOut, string? CostCode, string? VariationRef, string? Note)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static WorksElement? FromJson(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<WorksElement>(json, Options);
}
