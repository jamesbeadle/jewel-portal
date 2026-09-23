using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Public;

/// <summary>
/// A signature: the typed full name, kept as an answer under its own key, and the drawing on the
/// pad (wwwroot/js/signature-pad.js), which is sent as a PNG file when the form is. Clearing the
/// pad also forgets a drawing already sent, so what the office reads is what was last drawn.
/// </summary>
public partial class FormSignatureField
{
    private const string Pad = "jpmsSignaturePad";

    [Parameter, EditorRequired] public FormDefinition Form { get; set; } = default!;
    [Parameter, EditorRequired] public FormQuestion Question { get; set; } = default!;
    [Parameter, EditorRequired] public FormDraft Draft { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }
    [CascadingParameter] public FormSignaturePads? Pads { get; set; }

    private ElementReference canvas;
    private bool isUnavailable;

    private string NameKey => FormAnswerRules.SignatureNameKey(Question.Key);

    private string TypedName => Draft.AnswerTo(NameKey);

    protected override void OnInitialized() => Pads?.Add(Question.Key, this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try { await JS.InvokeVoidAsync($"{Pad}.attach", canvas); }
        catch (JSException) { isUnavailable = true; }
        if (isUnavailable) StateHasChanged();
    }

    /// <summary>The drawing as a PNG in base64, or null when nothing has been drawn.</summary>
    public async Task<string?> DrawingAsync()
    {
        try
        {
            var hasInk = await JS.InvokeAsync<bool>($"{Pad}.hasInk", canvas);
            return hasInk ? await JS.InvokeAsync<string>($"{Pad}.toPng", canvas) : null;
        }
        catch (JSException) { return null; }
    }

    private async Task ClearAsync()
    {
        try { await JS.InvokeVoidAsync($"{Pad}.clear", canvas); }
        catch (JSException) { isUnavailable = true; }
        Draft.ForgetFiles(Question.Key);
        await OnChanged.InvokeAsync();
    }

    private void NameTyped(ChangeEventArgs typed) => Draft.Answer(Form, NameKey, typed.Value?.ToString() ?? "");

    public void Dispose() => Pads?.Remove(Question.Key);
}
