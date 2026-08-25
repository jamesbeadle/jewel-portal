using System.Text.Json;

namespace Jewel.JPMS.Features.TenderEnquiries;

/// <summary>One question/answer row as the PQQ editor holds it, and the reader for the assistant's
/// {answers:[{question,answer}]} proposal (the tender_enquiry_answers dialog contract).</summary>
public sealed class TenderEnquiryAnswerRow
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";

    public bool IsBlank => string.IsNullOrWhiteSpace(Question) && string.IsNullOrWhiteSpace(Answer);

    /// <summary>The proposal's rows in order, or null when the JSON carries no usable answers.</summary>
    public static List<TenderEnquiryAnswerRow>? ParseProposal(string fieldsJson)
    {
        try
        {
            using var document = JsonDocument.Parse(fieldsJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty("answers", out var answers) || answers.ValueKind != JsonValueKind.Array) return null;
            var rows = new List<TenderEnquiryAnswerRow>();
            foreach (var item in answers.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var question = ReadString(item, "question");
                if (string.IsNullOrWhiteSpace(question)) continue;
                rows.Add(new TenderEnquiryAnswerRow { Question = question, Answer = ReadString(item, "answer") ?? "" });
            }
            return rows;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
