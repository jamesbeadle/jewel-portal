namespace Jewel.JPMS.Pages;

/// <summary>
/// The forms' first-day addresses (2026-09-23) carried a code before the form — /f/jbb/rtw,
/// /f/jbb/pack/{token} — until the second company was taken out the same day. A link sent in that
/// shape still lands: it is replaced by the form's own address, its one-time ?k= or ?p= kept.
/// </summary>
public partial class LegacyFormAddressRedirect
{
    [Parameter] public string Prefix { get; set; } = "";
    [Parameter] public string? Slug { get; set; }
    [Parameter] public string? Token { get; set; }

    private string NewAddress => Token is { } token ? PackAddress(token) : FormAddress(Slug ?? "");

    protected override void OnInitialized() => Nav.NavigateTo(NewAddress, replace: true);

    private static string PackAddress(string token) => $"/f/pack/{Uri.EscapeDataString(token)}";

    private string FormAddress(string slug)
    {
        var linkQuery = new Uri(Nav.Uri).Query;
        return $"/f/{Uri.EscapeDataString(slug)}{linkQuery}";
    }
}
