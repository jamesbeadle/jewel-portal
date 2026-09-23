namespace Jewel.JPMS.Api.Features.Forms.Public;

/// <summary>
/// A sentence meant for the person on the public page — "File too large.", "Please fill in: …" —
/// and the only refusal whose words leave the API. Anything else that goes wrong behind a public form
/// is logged and answered with the page's own "try again", so no setting or internal reason reaches
/// a stranger.
/// </summary>
public sealed class PublicFormRefusal : InvalidOperationException
{
    public PublicFormRefusal(string sentence) : base(sentence)
    {
    }
}
