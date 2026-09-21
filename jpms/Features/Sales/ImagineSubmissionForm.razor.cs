using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Features.Sales;

public partial class ImagineSubmissionForm
{
    private const long MaxUploadBytes = 6_000_000;
    private const int MaxPhotoEdge = 1600;

    private sealed record PendingPhoto(string FileName, string ContentType, string Base64, string Preview);

    [Parameter, EditorRequired] public string Token { get; set; } = "";
    [Parameter] public int MaxPhotosPerRound { get; set; } = ImagineLimits.MaxPhotosPerRound;
    [Parameter] public string ProspectEmail { get; set; } = "";
    [Parameter] public EventCallback<ImagineView> OnChanged { get; set; }
    [Parameter] public EventCallback<string?> OnError { get; set; }
    /// <summary>Raised once a round has been asked for, so the page starts watching for it.</summary>
    [Parameter] public EventCallback OnRoundRequested { get; set; }

    private bool busy;
    private bool preparing;
    private readonly List<PendingPhoto> photos = new();
    private string brief = "";
    private string name = "";
    private string email = "";
    private bool consent;
    private bool keepInTouch;

    private bool CanSubmit => photos.Count > 0 && !string.IsNullOrWhiteSpace(email) && consent;

    protected override void OnInitialized() => email = ProspectEmail;

    private async Task OnPhotosChosen(InputFileChangeEventArgs args)
    {
        await OnError.InvokeAsync(null);
        preparing = true;
        try
        {
            foreach (var file in args.GetMultipleFiles(MaxPhotosPerRound))
            {
                if (photos.Count >= MaxPhotosPerRound) break;
                await AddPhotoAsync(file);
            }
        }
        finally { preparing = false; }
    }

    private async Task AddPhotoAsync(IBrowserFile file)
    {
        try
        {
            // Shrunk in the browser: a phone photo is 3–8 MB; 1600px JPEG is a few hundred KB.
            var resized = await file.RequestImageFileAsync("image/jpeg", MaxPhotoEdge, MaxPhotoEdge);
            await using var stream = resized.OpenReadStream(MaxUploadBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var base64 = Convert.ToBase64String(buffer.ToArray());
            photos.Add(new PendingPhoto(file.Name, "image/jpeg", base64, "data:image/jpeg;base64," + base64));
        }
        catch
        {
            await OnError.InvokeAsync($"{file.Name} couldn't be read as a photo — JPEG or PNG from your camera roll works best.");
        }
    }

    private async Task SubmitAsync()
    {
        if (busy || !CanSubmit) return;
        busy = true;
        await OnError.InvokeAsync(null);
        try
        {
            var uploads = photos.Select(photo => new ImaginePhotoUpload(photo.FileName, photo.ContentType, photo.Base64)).ToList();
            var submission = new ImagineSubmission(name.Trim(), email.Trim(), brief.Trim(), uploads, consent, keepInTouch);
            var outcome = await ImagineRequests.PostAsync(Http, $"{ImagineRequests.BaseFor(Token)}/submit", submission);
            photos.Clear();
            if (outcome.Error is { } message) await OnError.InvokeAsync(message);
            if (outcome.View is { } updated) await OnChanged.InvokeAsync(updated);
            await OnRoundRequested.InvokeAsync();
        }
        finally { busy = false; }
    }
}
