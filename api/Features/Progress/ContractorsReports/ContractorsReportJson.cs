using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports;

/// <summary>The three small lists a report row keeps as JSON — Look Ahead, attendance and the
/// chosen update ids — written and read one way.</summary>
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
}
