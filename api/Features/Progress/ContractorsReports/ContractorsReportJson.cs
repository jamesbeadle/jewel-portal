using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports;

/// <summary>The small lists a report row keeps as JSON — Look Ahead, attendance, the chosen
/// update ids — and the one rewrite, written and read one way.</summary>
internal static class ContractorsReportJson
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Write<T>(IReadOnlyList<T> items) => JsonSerializer.Serialize(items, Json);

    public static IReadOnlyList<T> Read<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<T>();
        try { return JsonSerializer.Deserialize<List<T>>(json, Json) ?? new List<T>(); }
        catch (JsonException) { return Array.Empty<T>(); }
    }

    public static string WriteOne<T>(T item) => JsonSerializer.Serialize(item, Json);

    public static T? ReadOne<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, Json); }
        catch (JsonException) { return null; }
    }
}
