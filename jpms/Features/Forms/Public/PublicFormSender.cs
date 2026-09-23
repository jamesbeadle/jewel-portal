using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Public;

/// <summary>The one-time link a form was opened with, sent back with it so the answers carry an identity.</summary>
public sealed record FormLinkTokens(string? InviteToken, string? PackToken);

/// <summary>
/// Sending a form, in the dashboard's order: check it as the form would (one problem at a time,
/// the drawings counted as the signatures' files), send each new drawing as a PNG, then send the
/// answers with every file's id. The API checks it all again, so an old page cannot send what the
/// form would refuse.
/// </summary>
public sealed class PublicFormSender
{
    private readonly HttpClient http;
    private readonly FormDefinition form;
    private readonly FormLinkTokens tokens;

    public PublicFormSender(HttpClient http, FormDefinition form, FormLinkTokens tokens)
    {
        this.http = http;
        this.form = form;
        this.tokens = tokens;
    }

    public async Task<PublicFormAnswer<PublicFormReceipt>> SendAsync(FormDraft draft, FormSignaturePads pads)
    {
        var drawings = await pads.DrawingsAsync();
        var problem = FormAnswerRules.FirstProblem(form, draft.Answers, FileCountsWith(draft, drawings));
        if (problem is not null) return PublicFormAnswer<PublicFormReceipt>.Refused(problem);
        foreach (var (questionKey, drawing) in drawings) await SendDrawingAsync(draft, questionKey, drawing);
        var submission = new PublicFormSubmission(draft.SessionId, draft.Answers, draft.ArrivedFileIds(), tokens.InviteToken, tokens.PackToken);
        return await PublicFormRequests.SubmitAsync(http, form.Slug, submission);
    }

    private static Dictionary<string, int> FileCountsWith(FormDraft draft, IReadOnlyDictionary<string, string> drawings)
    {
        var counts = draft.ArrivedFileCounts();
        foreach (var questionKey in drawings.Keys) counts[questionKey] = counts.GetValueOrDefault(questionKey) + 1;
        return counts;
    }

    private async Task SendDrawingAsync(FormDraft draft, string questionKey, string drawing)
    {
        var isAlreadySent = draft.FilesFor(questionKey).Any(file => file.IsUploaded);
        if (isAlreadySent) return;
        var fileName = $"signature-{questionKey}.png";
        var upload = new PublicFormUpload(draft.SessionId, questionKey, fileName, drawing);
        var answer = await PublicFormRequests.UploadAsync(http, form.Slug, upload);
        if (answer.Value is not { } receipt) return;
        var sentDrawing = draft.AddFile(questionKey, receipt.FileName);
        sentDrawing.Arrived(receipt.FormUploadId, receipt.FileName);
    }
}
