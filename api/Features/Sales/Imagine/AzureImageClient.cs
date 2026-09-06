using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>
/// Azure OpenAI image generation (gpt-image-1) — the only image model behind the imagine renders.
/// Claude reads the photos and writes the concepts; this renders each concept over the prospect's
/// own photo through the image <i>edits</i> endpoint, so the result is their house, changed, not a
/// generic house. App settings (identical names on the SWA API and the worker):
/// <c>AzureImage__Endpoint</c> (https://{resource}.openai.azure.com), <c>AzureImage__ApiKey</c>,
/// <c>AzureImage__Deployment</c> (the deployment name; default gpt-image-1), optional
/// <c>AzureImage__ApiVersion</c>, <c>AzureImage__Size</c>, <c>AzureImage__Quality</c>.
/// Without an endpoint and key the null client answers with the reason and the round is stamped
/// Failed with it — nothing crashes.
/// </summary>
public sealed class AzureImageOptions
{
    public const string SectionName = "AzureImage";

    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string Deployment { get; set; } = "gpt-image-1";
    public string ApiVersion { get; set; } = "2025-04-01-preview";
    /// <summary>Landscape suits a house; gpt-image-1 accepts 1024x1024, 1536x1024, 1024x1536, auto.</summary>
    public string Size { get; set; } = "1536x1024";
    /// <summary>low / medium / high — medium is the cost/quality point for a first concept.</summary>
    public string Quality { get; set; } = "medium";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);

    public static AzureImageOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var options = new AzureImageOptions
        {
            Endpoint = section["Endpoint"]?.TrimEnd('/'),
            ApiKey = section["ApiKey"]
        };
        if (!string.IsNullOrWhiteSpace(section["Deployment"])) options.Deployment = section["Deployment"]!;
        if (!string.IsNullOrWhiteSpace(section["ApiVersion"])) options.ApiVersion = section["ApiVersion"]!;
        if (!string.IsNullOrWhiteSpace(section["Size"])) options.Size = section["Size"]!;
        if (!string.IsNullOrWhiteSpace(section["Quality"])) options.Quality = section["Quality"]!;
        return options;
    }
}

/// <summary>One reference image for an edit: bytes and their type.</summary>
public sealed record ImageInput(byte[] Bytes, string ContentType, string FileName);

/// <summary>A rendered image: JPEG/PNG bytes and their type.</summary>
public sealed record RenderedImage(byte[] Bytes, string ContentType);

public interface IAzureImageClient
{
    bool IsConfigured { get; }

    /// <summary>Render the prompt over the reference image(s). Throws with a readable reason on
    /// failure — the runner stamps the round with it.</summary>
    Task<RenderedImage> EditAsync(IReadOnlyList<ImageInput> references, string prompt, CancellationToken ct);
}

public sealed class AzureImageClient : IAzureImageClient
{
    private readonly HttpClient http;
    private readonly AzureImageOptions options;
    private readonly ILogger<AzureImageClient> logger;

    public AzureImageClient(HttpClient http, AzureImageOptions options, ILogger<AzureImageClient> logger)
    {
        this.http = http;
        this.options = options;
        this.logger = logger;
    }

    public bool IsConfigured => options.IsConfigured;

    public async Task<RenderedImage> EditAsync(IReadOnlyList<ImageInput> references, string prompt, CancellationToken ct)
    {
        if (!options.IsConfigured)
            throw new InvalidOperationException("Azure image generation isn't configured (AzureImage__Endpoint / AzureImage__ApiKey).");
        if (references.Count == 0)
            throw new InvalidOperationException("An image edit needs at least one reference photo.");

        var url = $"{options.Endpoint}/openai/deployments/{Uri.EscapeDataString(options.Deployment)}/images/edits?api-version={Uri.EscapeDataString(options.ApiVersion)}";

        using var form = new MultipartFormDataContent();
        // gpt-image-1 takes several reference images as an array part; a single one as "image".
        var fieldName = references.Count == 1 ? "image" : "image[]";
        foreach (var reference in references)
        {
            var part = new ByteArrayContent(reference.Bytes);
            part.Headers.ContentType = new MediaTypeHeaderValue(reference.ContentType);
            form.Add(part, fieldName, reference.FileName);
        }
        form.Add(new StringContent(prompt), "prompt");
        form.Add(new StringContent(options.Size), "size");
        form.Add(new StringContent(options.Quality), "quality");
        form.Add(new StringContent("1"), "n");
        form.Add(new StringContent("jpeg"), "output_format");

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        request.Headers.Add("api-key", options.ApiKey);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await SafeBodyAsync(response, ct);
            logger.LogWarning("Azure image edit failed: {Status}. {Detail}", (int)response.StatusCode, detail);
            throw new InvalidOperationException(
                $"Azure image generation answered {(int)response.StatusCode}. {Trim(ErrorMessage(detail), 300)}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
            throw new InvalidOperationException("Azure image generation returned no image.");
        var first = data[0];
        if (!first.TryGetProperty("b64_json", out var b64) || b64.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Azure image generation returned an image without bytes.");
        var bytes = Convert.FromBase64String(b64.GetString()!);
        var contentType = bytes.Length > 4 && bytes[0] == 0x89 && bytes[1] == 0x50 ? "image/png" : "image/jpeg";
        return new RenderedImage(bytes, contentType);
    }

    private static string ErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message))
                    return message.GetString() ?? body;
                if (error.ValueKind == JsonValueKind.String) return error.GetString() ?? body;
            }
        }
        catch (JsonException) { }
        return body;
    }

    private static async Task<string> SafeBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); } catch { return "(no body)"; }
    }

    private static string Trim(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}

public sealed class NullAzureImageClient : IAzureImageClient
{
    public bool IsConfigured => false;

    public Task<RenderedImage> EditAsync(IReadOnlyList<ImageInput> references, string prompt, CancellationToken ct) =>
        Task.FromException<RenderedImage>(new InvalidOperationException(
            "Azure image generation isn't configured (AzureImage__Endpoint / AzureImage__ApiKey / AzureImage__Deployment)."));
}
