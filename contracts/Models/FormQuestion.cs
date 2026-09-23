namespace Jewel.JPMS.Models;

/// <summary>What a question asks for. Values are persisted in no table (definitions live in code),
/// but they travel as JSON, so they are appended, never reordered.</summary>
public enum FormQuestionKind
{
    Text = 0,
    LongText = 1,
    Choice = 2,
    Date = 3,
    DateAndTime = 4,
    Upload = 5,
    Signature = 6,
    Declaration = 7,
    Section = 8
}

/// <summary>A question asked only while another question holds a given answer ("If yes, give details").</summary>
public sealed record FormCondition(string QuestionKey, string Answer);

/// <summary>
/// One question on a form, carried from the JPS Dashboard's field definitions: its key, the words
/// the person reads, whether it must be answered, its hint, the choices, and when it is shown. A
/// Section is a heading and never an answer. IsSpecialCategory marks health data under UK GDPR
/// Article 9 that the office sees only when it is revealed.
/// </summary>
public sealed record FormQuestion(
    string Key,
    string Label,
    FormQuestionKind Kind,
    bool IsRequired = false,
    string Hint = "",
    IReadOnlyList<string>? Choices = null,
    FormCondition? ShownWhen = null,
    bool IsSpecialCategory = false)
{
    public IReadOnlyList<string> Choices { get; init; } = Choices ?? Array.Empty<string>();

    public bool IsAsked => Kind != FormQuestionKind.Section;

    public bool IsShownFor(IReadOnlyDictionary<string, string> answers) =>
        ShownWhen is null || answers.GetValueOrDefault(ShownWhen.QuestionKey) == ShownWhen.Answer;
}

/// <summary>The words a definition is written in: one line per question, read like the dashboard's arrays.</summary>
public static class Ask
{
    public const bool Required = true;
    public const bool Optional = false;

    public static FormQuestion Text(string key, string label, bool isRequired, string hint = "") =>
        new(key, label, FormQuestionKind.Text, isRequired, hint);

    public static FormQuestion LongText(string key, string label, bool isRequired, string hint = "") =>
        new(key, label, FormQuestionKind.LongText, isRequired, hint);

    public static FormQuestion Choice(string key, string label, bool isRequired, string[] choices, string hint = "") =>
        new(key, label, FormQuestionKind.Choice, isRequired, hint, choices);

    public static FormQuestion Date(string key, string label, bool isRequired, string hint = "") =>
        new(key, label, FormQuestionKind.Date, isRequired, hint);

    public static FormQuestion DateAndTime(string key, string label, bool isRequired, string hint = "") =>
        new(key, label, FormQuestionKind.DateAndTime, isRequired, hint);

    public static FormQuestion Upload(string key, string label, bool isRequired, string hint = "") =>
        new(key, label, FormQuestionKind.Upload, isRequired, hint);

    public static FormQuestion Signature(string key, string label, bool isRequired, string hint = "") =>
        new(key, label, FormQuestionKind.Signature, isRequired, hint);

    public static FormQuestion Declaration(string key, string label, bool isRequired) =>
        new(key, label, FormQuestionKind.Declaration, isRequired);

    public static FormQuestion Section(string key, string label, string hint = "") =>
        new(key, label, FormQuestionKind.Section, Optional, hint);
}
