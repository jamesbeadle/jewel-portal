namespace Jewel.JPMS.Features.Requests;

/// <summary>What the request editors share when reading their fields back: a date box's text as
/// a date, and blank-means-null.</summary>
public static class RequestEditParsing
{
    public static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
