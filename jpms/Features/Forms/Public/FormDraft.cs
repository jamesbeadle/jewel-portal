using System.Security.Cryptography;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Public;

/// <summary>
/// A form being filled in on the page: the session its files arrive under, the answers by question
/// key, and the files chosen for each question. An answer to a question that stops being asked is
/// cleared, as the dashboard's page cleared a box when the answer it hung on changed back.
/// </summary>
public sealed class FormDraft
{
    private readonly Dictionary<string, string> answers;
    private readonly Dictionary<string, List<FormFileState>> files;

    private FormDraft(string sessionId, Dictionary<string, string> answers, Dictionary<string, List<FormFileState>> files)
    {
        SessionId = sessionId;
        this.answers = answers;
        this.files = files;
    }

    public string SessionId { get; }

    public IReadOnlyDictionary<string, string> Answers => answers;

    public static FormDraft Start(IReadOnlyDictionary<string, string> prefills) =>
        new(NewSessionId(), new Dictionary<string, string>(prefills), new Dictionary<string, List<FormFileState>>());

    public static FormDraft Resume(FormDraftRecord record) =>
        new(record.SessionId, new Dictionary<string, string>(record.Answers), record.Files.ToDictionary(
            pair => pair.Key, pair => pair.Value.Select(file => new FormFileState(file.FileName, file.FormUploadId)).ToList()));

    public string AnswerTo(string key) => answers.GetValueOrDefault(key, "");

    public void Answer(FormDefinition form, string key, string value)
    {
        answers[key] = value;
        var noLongerAsked = form.Questions.Where(question => !question.IsShownFor(answers)).Select(question => question.Key).ToList();
        foreach (var hiddenKey in noLongerAsked) answers.Remove(hiddenKey);
    }

    public IReadOnlyList<FormFileState> FilesFor(string key) =>
        files.TryGetValue(key, out var chosen) ? chosen : Array.Empty<FormFileState>();

    public FormFileState AddFile(string key, string fileName)
    {
        var file = new FormFileState(fileName);
        var chosen = files.TryGetValue(key, out var existing) ? existing : files[key] = new List<FormFileState>();
        chosen.Add(file);
        return file;
    }

    public void ForgetFiles(string key) => files.Remove(key);

    public Dictionary<string, int> ArrivedFileCounts() =>
        files.ToDictionary(pair => pair.Key, pair => pair.Value.Count(file => file.IsUploaded));

    public Dictionary<string, IReadOnlyList<string>> ArrivedFileIds() =>
        files.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)ArrivedIn(pair.Value).Select(file => file.FormUploadId).ToList());

    /// <summary>What the browser may keep: never an answer the form treats as sensitive, which is typed again after a reload.</summary>
    public FormDraftRecord ToRecord(FormDefinition form, DateTimeOffset now) =>
        new(SessionId, answers.Where(pair => !IsSensitive(form, pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value), files.ToDictionary(
            pair => pair.Key, pair => ArrivedIn(pair.Value).Select(file => new FormDraftFile(file.FormUploadId, file.FileName)).ToList()), now);

    private static bool IsSensitive(FormDefinition form, string key) => form.QuestionFor(key) is { } question && SensitiveAnswers.IsSensitive(question);

    private static IEnumerable<(string FormUploadId, string FileName)> ArrivedIn(IEnumerable<FormFileState> chosen) =>
        chosen.Where(file => file.IsUploaded).Select(file => (file.FormUploadId!, file.FileName));

    private static string NewSessionId() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(PublicFormLimits.SessionIdBytes)).ToLowerInvariant();
}

/// <summary>
/// The part of a draft the browser keeps across a reload: the session, the answers that are safe to leave on a
/// phone, the files that arrived, and when it was kept — a draft older than a month is not picked up again.
/// </summary>
public sealed record FormDraftRecord(
    string SessionId, Dictionary<string, string> Answers, Dictionary<string, List<FormDraftFile>> Files, DateTimeOffset KeptAt);

public sealed record FormDraftFile(string FormUploadId, string FileName);
