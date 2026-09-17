namespace Jewel.JPMS.Features.Sales;

public partial class ImagineRoundList
{
    [Parameter, EditorRequired] public string Token { get; set; } = "";
    [Parameter, EditorRequired] public ImagineView View { get; set; } = default!;
    [Parameter] public EventCallback<ImagineView> OnChanged { get; set; }
    [Parameter] public EventCallback<string?> OnError { get; set; }
    /// <summary>Raised once a revision has been asked for, so the page starts watching for it.</summary>
    [Parameter] public EventCallback OnRoundRequested { get; set; }

    private bool busy;
    private ImagineImageView? revising;
    private string feedback = "";
    private readonly Dictionary<string, string> commentDrafts = new();

    private string Base => ImagineRequests.BaseFor(Token);
    private bool AnyInProgress => View.Rounds.Any(round => round.Status.IsInProgress());
    private string CommentDraft(ImagineImageView concept) => commentDrafts.TryGetValue(concept.ImageId, out var draft) ? draft : concept.Comment;

    // Literal class strings so the Tailwind build sees them.
    private static string ConceptGridClass(int count) => count switch
    {
        <= 1 => "grid grid-cols-1 gap-5 max-w-2xl",
        2 => "grid grid-cols-1 md:grid-cols-2 gap-5",
        _ => "grid grid-cols-1 md:grid-cols-3 gap-5"
    };

    private void OpenRevise(ImagineImageView concept)
    {
        revising = concept;
        feedback = "";
    }

    private async Task ReviseAsync()
    {
        if (busy || revising is null || string.IsNullOrWhiteSpace(feedback)) return;
        busy = true;
        try
        {
            await PostAsync($"{Base}/revise", new ImagineRevisionRequest(revising.ImageId, feedback.Trim()));
            revising = null;
            feedback = "";
            await OnRoundRequested.InvokeAsync();
        }
        finally { busy = false; }
    }

    private async Task ToggleLikeAsync(ImagineImageView concept)
    {
        if (busy) return;
        busy = true;
        try { await PostAsync($"{Base}/react", new ImagineReaction(concept.ImageId, !concept.Liked, CommentDraft(concept))); }
        finally { busy = false; }
    }

    private async Task SaveCommentAsync(ImagineImageView concept)
    {
        if (busy) return;
        busy = true;
        try
        {
            await PostAsync($"{Base}/react", new ImagineReaction(concept.ImageId, concept.Liked, CommentDraft(concept)));
            commentDrafts.Remove(concept.ImageId);
        }
        finally { busy = false; }
    }

    private async Task PostAsync<T>(string url, T body)
    {
        await OnError.InvokeAsync(null);
        var outcome = await ImagineRequests.PostAsync(Http, url, body);
        if (outcome.Error is { } message) await OnError.InvokeAsync(message);
        if (outcome.View is { } updated) await OnChanged.InvokeAsync(updated);
    }
}
