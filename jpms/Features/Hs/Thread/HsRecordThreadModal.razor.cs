using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Features.Hs.Thread;

public partial class HsRecordThreadModal
{
    private const string CommentHintForOwner = "Say where the action has got to, or what is stopping you. The H&S officer is told.";
    private const string CommentHintForOfficer = "Answer, or note what you saw. The site manager is told.";

    [Parameter] public HsRecord? Record { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<HsStatus> OnStatusChanged { get; set; }
    [Parameter] public EventCallback OnCommented { get; set; }

    private IReadOnlyList<HsRecordComment>? comments;
    private IReadOnlyList<IBrowserFile> chosenFiles = Array.Empty<IBrowserFile>();
    private string draft = "";
    private string? problem;
    private bool isSending;
    private string loadedForRecordId = "";

    private string Title => Record is { } record ? record.Kind.KindDisplayName() : "";

    private bool MayClose => Session.CanOpen(HsActionRoles.AllowedToClose);

    private bool MayComment => Session.CanOpen(HsActionRoles.Contributors);

    private string CommentHint => MayClose ? CommentHintForOfficer : CommentHintForOwner;

    private bool CanSend => !isSending && (draft.Trim().Length > 0 || chosenFiles.Count > 0);

    private string ChosenFilesLabel => chosenFiles.Count switch
    {
        0 => "Add a photo",
        1 => "1 photo chosen",
        _ => $"{chosenFiles.Count} photos chosen"
    };

    protected override async Task OnParametersSetAsync()
    {
        var recordId = Record?.HsRecordId ?? "";
        if (recordId == loadedForRecordId) return;
        loadedForRecordId = recordId;
        comments = null;
        problem = null;
        draft = "";
        chosenFiles = Array.Empty<IBrowserFile>();
        if (recordId.Length > 0) await LoadAsync(recordId);
    }

    private async Task LoadAsync(string recordId)
    {
        try { comments = await Thread.ThreadAsync(recordId, CancellationToken.None); }
        catch (Exception unreadable) when (unreadable is not OperationCanceledException) { problem = "The thread could not be read — close and open the action again."; }
    }

    private void FilesChosen(InputFileChangeEventArgs chosen) => chosenFiles = chosen.GetMultipleFiles().ToList();

    private async Task SendAsync()
    {
        if (Record is not { } record) return;
        isSending = true;
        problem = null;
        try
        {
            await PostAsync(record.HsRecordId);
            draft = "";
            chosenFiles = Array.Empty<IBrowserFile>();
            await LoadAsync(record.HsRecordId);
            await OnCommented.InvokeAsync();
        }
        catch (CommandFailedException refusal) { problem = refusal.Message; }
        catch (InvalidOperationException refusal) { problem = refusal.Message; }
        finally { isSending = false; }
    }

    private async Task PostAsync(string recordId)
    {
        var hasPhotos = chosenFiles.Count > 0;
        if (hasPhotos) await Thread.CommentWithPhotosAsync(recordId, draft.Trim(), chosenFiles, CancellationToken.None);
        if (!hasPhotos) await Thread.CommentAsync(recordId, draft.Trim(), CancellationToken.None);
    }
}
