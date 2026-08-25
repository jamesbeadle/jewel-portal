using System.Text.Json;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations;

/// <summary>
/// The staged build-up's JSON shape on VariationOrderEntity.DraftLinesJson: an array of
/// {costCode, description, quantity, rate}. One serialiser, one parser; a corrupt column reads as
/// "nothing staged" rather than taking the record page down.
/// </summary>
internal static class VariationDraftLines
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private sealed record StoredLine(string CostCode, string Description, decimal Quantity, decimal Rate);

    public static string? Serialise(IReadOnlyList<VariationLineInput> lines)
    {
        if (lines.Count == 0) return null;
        return JsonSerializer.Serialize(
            lines.Select(line => new StoredLine(line.CostCode.Trim(), (line.Description ?? "").Trim(), line.Quantity, line.Rate)).ToList(),
            Json);
    }

    public static IReadOnlyList<VariationLineInput>? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var stored = JsonSerializer.Deserialize<List<StoredLine>>(json, Json);
            if (stored is null || stored.Count == 0) return null;
            // A hand-edited column with a blank cost code is not a line the panel can seed.
            var lines = stored
                .Where(line => !string.IsNullOrWhiteSpace(line.CostCode))
                .Select(line => new VariationLineInput(line.CostCode, line.Description ?? "", line.Quantity, line.Rate))
                .ToList();
            return lines.Count == 0 ? null : lines;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
