using Jewel.JPMS.Api.Features.Ai;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>One concept Claude wrote: what to call it, the idea for the prospect, and the prompt
/// the image model renders it from.</summary>
public sealed record ImagineConcept(string Title, string Description, string ImagePrompt);

/// <summary>What Claude made of a round: its reading of the photos, and the concepts.</summary>
public sealed record ImagineConceptSet(string Observations, IReadOnlyList<ImagineConcept> Concepts);

/// <summary>
/// The Claude side of a render: looks at the prospect's photos (image blocks on one Messages
/// call), reads their brief, and writes the concepts — an architect's eye on what the house or
/// plot could become, each with a precise image-generation prompt that keeps the real house
/// recognisable and changes only what the concept changes. A revision round sees the chosen
/// concept, the photos and the prospect's notes and writes the variants. Throws on failure so
/// the runner stamps the round Failed with the reason.
/// </summary>
public sealed class ImagineConceptWriter
{
    private const string MessagesUrl = "https://api.anthropic.com/v1/messages";
    private const int MaxTokens = 3000;

    private readonly HttpClient http;
    private readonly AnthropicOptions options;
    private readonly ILogger<ImagineConceptWriter> logger;

    public ImagineConceptWriter(HttpClient http, AnthropicOptions options, ILogger<ImagineConceptWriter> logger)
    {
        this.http = http;
        this.options = options;
        this.logger = logger;
    }

    public bool IsConfigured => options.IsConfigured;

    public async Task<ImagineConceptSet> WriteAsync(
        IReadOnlyList<ImageInput> photos,
        ImageInput? chosenConcept,
        string? chosenConceptTitle,
        string? chosenConceptPrompt,
        string brief,
        string propertyLine,
        int conceptCount,
        CancellationToken ct)
    {
        if (!options.IsConfigured)
            throw new InvalidOperationException("No Anthropic API key is configured on the worker (Anthropic__ApiKey).");

        var content = new List<object>();
        foreach (var photo in photos)
        {
            content.Add(new
            {
                type = "image",
                source = new { type = "base64", media_type = photo.ContentType, data = Convert.ToBase64String(photo.Bytes) }
            });
        }
        if (chosenConcept is not null)
        {
            content.Add(new { type = "text", text = "The concept the prospect chose to build on:" });
            content.Add(new
            {
                type = "image",
                source = new { type = "base64", media_type = chosenConcept.ContentType, data = Convert.ToBase64String(chosenConcept.Bytes) }
            });
        }
        content.Add(new { type = "text", text = UserPrompt(photos.Count, chosenConceptTitle, chosenConceptPrompt, brief, propertyLine, conceptCount) });

        using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl)
        {
            Content = JsonContent.Create(new
            {
                model = options.ModelForTier("sonnet"),
                max_tokens = MaxTokens,
                system = SystemPrompt,
                messages = new[] { new { role = "user", content } }
            })
        };
        request.Headers.Add("x-api-key", options.ApiKey);
        request.Headers.Add("anthropic-version", options.ApiVersion);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await SafeBodyAsync(response, ct);
            logger.LogWarning("Imagine concept call failed: {Status}. {Detail}", (int)response.StatusCode, detail);
            throw new InvalidOperationException($"Claude answered {(int)response.StatusCode} to the concept call. {Trim(detail, 300)}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var answer = FinalTextBlock(doc.RootElement)
            ?? throw new InvalidOperationException("Claude returned no written answer from the concept call.");
        return Parse(answer, conceptCount);
    }

    private const string SystemPrompt =
        "You are the design eye of Jewel Bespoke Build, Surrey's super-prime residential builder: "
        + "bespoke new homes and substantial upgrades — extensions, remodels, whole-house refurbishments, "
        + "new builds on a plot — for private clients. A prospect has photographed their house or plot "
        + "and written what they dream of. Look at the photographs properly: the style and era, the "
        + "materials (brick, render, tile-hanging, timber, stone), the roof form, the windows, the "
        + "garden and its levels, the neighbours and the street, the light. Then write concepts an "
        + "architect would be proud of and a planning officer could live with — ambitious, specific to "
        + "THIS house, and buildable. British English, no hype. Answer with STRICT JSON only — no "
        + "prose before or after, no markdown fences:\n"
        + "{\"observations\":\"one warm paragraph (60-120 words), addressed to the prospect, on what "
        + "you saw in their photos and what the property lends itself to\","
        + "\"concepts\":[{\"title\":\"3-6 words\",\"description\":\"2-4 sentences for the prospect: the "
        + "idea, what changes, why it suits the house\",\"imagePrompt\":\"the instruction to an image "
        + "model that will EDIT the prospect's own photograph — 60-120 words, present tense, "
        + "photorealistic: keep the camera angle, the existing house, the neighbours, the garden and the "
        + "sky exactly as photographed; change ONLY what the concept changes, naming materials, "
        + "proportions, glazing, roof form and landscaping precisely; daylight, no people, no text\"}]}\n"
        + "Each concept must be genuinely different in ambition or direction (e.g. a contemporary rear "
        + "extension; a full remodel with new elevations; a new-build replacement), not three shades "
        + "of the same idea. When revising a chosen concept, keep what they liked and change what they "
        + "asked for — the variants differ in how they interpret the notes.";

    private static string UserPrompt(int photoCount, string? chosenTitle, string? chosenPrompt, string brief, string propertyLine, int conceptCount)
    {
        var lines = new List<string>
        {
            $"Property: {(string.IsNullOrWhiteSpace(propertyLine) ? "(address not given)" : propertyLine)}",
            $"Photographs supplied: {photoCount}",
            ""
        };
        if (chosenTitle is not null)
        {
            lines.Add($"THE CONCEPT THEY CHOSE: \"{chosenTitle}\"");
            lines.Add($"It was rendered from this prompt: {chosenPrompt}");
            lines.Add("");
            lines.Add("WHAT THEY WOULD CHANGE (their notes):");
            lines.Add(string.IsNullOrWhiteSpace(brief) ? "(no notes — refine and improve the chosen concept)" : brief);
            lines.Add("");
            lines.Add($"Write {conceptCount} revised variants of the chosen concept, each honouring their notes, as the JSON.");
        }
        else
        {
            lines.Add("WHAT THEY DREAM OF (their brief):");
            lines.Add(string.IsNullOrWhiteSpace(brief) ? "(no brief written — propose what the house most wants)" : brief);
            lines.Add("");
            lines.Add($"Write {conceptCount} concepts for this property as the JSON.");
        }
        return string.Join("\n", lines);
    }

    private static string? FinalTextBlock(JsonElement root)
    {
        if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            return null;
        string? last = null;
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type) && type.GetString() == "text"
                && block.TryGetProperty("text", out var text))
                last = text.GetString();
        }
        return last;
    }

    private static ImagineConceptSet Parse(string answer, int expected)
    {
        var json = answer.Trim();
        var start = json.IndexOf('{');
        var end = json.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Claude's concept answer was not JSON.");
        json = json[start..(end + 1)];

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var observations = root.TryGetProperty("observations", out var obs) && obs.ValueKind == JsonValueKind.String
                ? obs.GetString() ?? "" : "";
            var concepts = new List<ImagineConcept>();
            if (root.TryGetProperty("concepts", out var array) && array.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in array.EnumerateArray())
                {
                    var title = Str(item, "title");
                    var description = Str(item, "description");
                    var prompt = Str(item, "imagePrompt");
                    if (string.IsNullOrWhiteSpace(prompt)) continue;
                    concepts.Add(new ImagineConcept(
                        string.IsNullOrWhiteSpace(title) ? $"Concept {concepts.Count + 1}" : title.Trim(),
                        description.Trim(),
                        prompt.Trim()));
                    if (concepts.Count == expected) break;
                }
            }
            if (concepts.Count == 0)
                throw new InvalidOperationException("Claude wrote no usable concepts.");
            return new ImagineConceptSet(observations.Trim(), concepts);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Claude's concept answer could not be read: {ex.Message}");
        }
    }

    private static string Str(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static async Task<string> SafeBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); } catch { return "(no body)"; }
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max] + "…";
}
