using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Public;

/// <summary>
/// A question that takes files. Each file chosen is sent on its own straight away, before the form,
/// so a big photograph never holds the answers hostage and a dropped signal loses one file, not the
/// form (the dashboard's op:'file'); its line under the button says where it has got to.
/// </summary>
public partial class FormUploadField
{
    [Parameter, EditorRequired] public FormQuestion Question { get; set; } = default!;
    [Parameter, EditorRequired] public FormDraft Draft { get; set; } = default!;
    [Parameter, EditorRequired] public string CompanyCode { get; set; } = "";
    [Parameter, EditorRequired] public string Slug { get; set; } = "";
    [Parameter] public EventCallback OnChanged { get; set; }

    private string? problem;

    private async Task SendChosenAsync(InputFileChangeEventArgs chosen)
    {
        problem = null;
        var isTooMany = chosen.FileCount > PublicFormLimits.MostFilesPerSession;
        if (isTooMany) problem = FormWording.TooManyFiles;
        if (isTooMany) return;
        foreach (var file in chosen.GetMultipleFiles(PublicFormLimits.MostFilesPerSession)) await SendAsync(file);
    }

    private async Task SendAsync(IBrowserFile chosen)
    {
        var isTooBig = chosen.Size > PublicFormLimits.LargestFileChosen;
        if (isTooBig) problem = FormWording.TooBig(chosen.Name);
        if (isTooBig) return;
        var line = Draft.AddFile(Question.Key, chosen.Name);
        StateHasChanged();
        var (prepared, refusal) = await TryPrepareAsync(chosen);
        var reason = refusal ?? FormWording.UploadFailed(chosen.Name);
        if (prepared is null) line.Failed(reason);
        if (prepared is null) return;
        var upload = new PublicFormUpload(Draft.SessionId, Question.Key, prepared.FileName, prepared.Base64);
        var answer = await PublicFormRequests.UploadAsync(Http, CompanyCode, Slug, upload);
        Record(line, prepared, answer);
        await OnChanged.InvokeAsync();
    }

    private void Record(FormFileState line, PreparedFile prepared, PublicFormAnswer<PublicFormUploadReceipt> answer)
    {
        if (answer.Value is { } receipt) line.Arrived(receipt.FormUploadId, receipt.FileName);
        if (answer.Value is not null) return;
        line.Failed(FormWording.UploadFailed(prepared.FileName));
        var isOnlyTheSignal = answer.Problem == FormWording.NoSignal;
        problem = isOnlyTheSignal ? null : answer.Problem;
    }

    private static async Task<(PreparedFile? File, string? Problem)> TryPrepareAsync(IBrowserFile chosen)
    {
        try { return await FormFilePreparation.PrepareAsync(chosen); }
        catch (Exception unreadable) when (unreadable is IOException or JSException or InvalidOperationException)
        {
            return (null, FormWording.UploadFailed(chosen.Name));
        }
    }
}
