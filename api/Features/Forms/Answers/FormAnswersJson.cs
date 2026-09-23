namespace Jewel.JPMS.Api.Features.Forms.Answers;

/// <summary>A submission's answers as stored: one JSON object of question key to answer, never queried.</summary>
internal static class FormAnswersJson
{
    public static string Write(IReadOnlyDictionary<string, string> answers) => JsonSerializer.Serialize(answers);

    public static Dictionary<string, string> Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }
}
