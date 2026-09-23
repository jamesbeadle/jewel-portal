namespace Jewel.JPMS.Features.Forms.Public;

/// <summary>
/// The signature boxes on a form's page, by question, cascaded to them so the page can read every
/// drawing at the moment it is sent — the dashboard's SIGPADS. A drawing is only ever read then:
/// a box nobody drew in has nothing to send.
/// </summary>
public sealed class FormSignaturePads
{
    private readonly Dictionary<string, FormSignatureField> pads = new();

    public void Add(string questionKey, FormSignatureField pad) => pads[questionKey] = pad;

    public void Remove(string questionKey) => pads.Remove(questionKey);

    public async Task<Dictionary<string, string>> DrawingsAsync()
    {
        var drawings = new Dictionary<string, string>();
        foreach (var (questionKey, pad) in pads) await AddDrawingAsync(drawings, questionKey, pad);
        return drawings;
    }

    private static async Task AddDrawingAsync(Dictionary<string, string> drawings, string questionKey, FormSignatureField pad)
    {
        var drawing = await pad.DrawingAsync();
        if (drawing is not null) drawings[questionKey] = drawing;
    }
}
