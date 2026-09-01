using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// Turns Bluebeam's markups payload into DrawingMarkup rows. The GET /publicapi/v2 …/markups
/// response is a bare array of SessionMarkupDto (verified against the live developer-portal
/// OpenAPI, 2026-08-31: camelCase — markupId, type, subject, comments, email/displayName,
/// pageNumber, created/modified, length, unit, x, y, status, label, layer). Still deliberately
/// lossy-safe: the array is also found under a wrapper property, names are matched
/// case-insensitively with aliases, and every markup's whole object is kept verbatim in RawJson —
/// a field this parser doesn't know about is recoverable later without re-extracting. A payload
/// that matches nothing yields zero rows, never a throw: the raw blob is the ground truth.
/// </summary>
public static class BluebeamMarkupParser
{
    public static List<DrawingMarkupEntity> Parse(string rawJson, string extractionId, string revisionId)
    {
        using var document = ParseDocument(rawJson);
        if (document is null) return new List<DrawingMarkupEntity>();

        var markups = FindMarkupArray(document.RootElement);
        if (markups is null) return new List<DrawingMarkupEntity>();

        var rows = new List<DrawingMarkupEntity>();
        foreach (var markup in markups.Value.EnumerateArray())
        {
            if (markup.ValueKind != JsonValueKind.Object) continue;
            rows.Add(ToRow(markup, extractionId, revisionId));
        }
        return rows;
    }

    private static JsonDocument? ParseDocument(string rawJson)
    {
        try { return JsonDocument.Parse(rawJson); }
        catch (JsonException) { return null; }
    }

    private static JsonElement? FindMarkupArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array) return root;
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var name in new[] { "Markups", "Items", "Data", "Results" })
        {
            foreach (var property in root.EnumerateObject())
            {
                var matches = property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.Array;
                if (matches) return property.Value;
            }
        }
        return null;
    }

    private static DrawingMarkupEntity ToRow(JsonElement markup, string extractionId, string revisionId) =>
        new()
        {
            DrawingMarkupId = Guid.NewGuid().ToString("N"),
            DrawingExtractionId = extractionId,
            DrawingRevisionId = revisionId,
            BluebeamMarkupId = Take(Text(markup, "markupId", "id", "guid"), 128),
            PageNumber = Number(markup, "pageNumber", "page") ?? 0,
            MarkupType = Take(Text(markup, "type", "markupType", "subtype"), 64),
            Subject = Take(Text(markup, "subject"), 256),
            Author = Take(Text(markup, "displayName", "author", "email", "createdBy"), 256),
            Comment = Take(Text(markup, "comments", "comment", "contents", "text", "label"), 4000),
            Colour = Take(Text(markup, "color", "colour"), 32),
            CreatedAtRaw = Take(Text(markup, "created", "createdDate", "creationDate"), 64),
            ModifiedAtRaw = Take(Text(markup, "modified", "modifiedDate", "lastModified"), 64),
            // "length" is the measurement reading on measurement markups ("12.5" as text); a value
            // that doesn't parse as a plain number stays recoverable in RawJson.
            MeasurementValue = Decimal(markup, "length", "measurement", "measurementValue", "value"),
            MeasurementUnit = NullIfEmpty(Take(Text(markup, "unit", "units", "measurementUnit"), 32)),
            RectJson = NullIfEmpty(Take(PositionJson(markup), 512)),
            RawJson = markup.GetRawText()
        };

    private static string Text(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
                if (property.Value.ValueKind == JsonValueKind.String) return property.Value.GetString() ?? "";
                if (property.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                    return property.Value.GetRawText();
            }
        }
        return "";
    }

    // The list DTO carries a position (x, y) rather than a bounding rectangle — kept as a small
    // JSON object so a future diff can still say "moved". A rect-shaped field wins when present.
    private static string PositionJson(JsonElement markup)
    {
        var rect = Raw(markup, "rect", "rectangle", "boundingBox");
        if (rect.Length > 0) return rect;
        var x = Text(markup, "x");
        var y = Text(markup, "y");
        if (x.Length == 0 && y.Length == 0) return "";
        return $"{{\"x\":{(x.Length > 0 ? x : "null")},\"y\":{(y.Length > 0 ? y : "null")}}}";
    }

    private static string Raw(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return property.Value.GetRawText();
            }
        }
        return "";
    }

    private static int? Number(JsonElement element, params string[] names)
    {
        var text = Text(element, names);
        return int.TryParse(text, out var value) ? value : null;
    }

    private static decimal? Decimal(JsonElement element, params string[] names)
    {
        var text = Text(element, names);
        return decimal.TryParse(
            text, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string Take(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private static string? NullIfEmpty(string value) =>
        value.Length == 0 ? null : value;
}
