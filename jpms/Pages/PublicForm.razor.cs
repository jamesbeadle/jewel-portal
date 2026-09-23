using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Features.Forms.Public;

namespace Jewel.JPMS.Pages;

/// <summary>
/// A public form (/f/{company}/{form}) — what a new starter or a sub-contractor fills in on a phone
/// with no account, carried from the JPS Dashboard's /forms/{slug}. It opens by its open address, by
/// a one-time link for one named person (?k=), or from a new starter's pack (?p=); a dead link shows
/// why instead of a form filled in for nothing. The questions come from the shared definitions, so
/// the page, the API and the office read one form.
/// </summary>
public partial class PublicForm
{
    [Parameter] public string Company { get; set; } = "";
    [Parameter] public string Slug { get; set; } = "";
    [SupplyParameterFromQuery(Name = "k")] public string? InviteToken { get; set; }
    [SupplyParameterFromQuery(Name = "p")] public string? PackToken { get; set; }

    private readonly FormSignaturePads pads = new();
    private PublicFormView? view;
    private FormDraft? draft;
    private string? openProblem;
    private string? sendProblem;
    private PublicFormReceipt? receipt;
    private bool isSending;

    private JewelCompanyParticulars? Particulars => JewelCompanies.ForCode(Company);

    private FormDefinition? Form => FormCatalogue.For(Slug);

    private FormNotice? DeadLink => view?.Problem is { } problem ? FormWording.DeadLink(problem, Particulars!.Phone) : null;

    private string Title => DeadLink?.Heading ?? Form!.TitleFor(Particulars!.Company);

    private string Intro => DeadLink?.Body ?? Form!.Intro;

    private string PageTitleText => Particulars is { } company && Form is not null ? $"{Title} - {company.ShortName}" : FormSheetWording.NotAvailable;

    private bool IsFillingIn => draft is not null && receipt is null;

    private string DraftKey => FormDraftStorage.KeyFor(Particulars!.Code, Form!.Slug, PackToken ?? InviteToken);

    private string? PackAddress =>
        string.IsNullOrEmpty(PackToken) ? null : $"/f/{Particulars!.Code}/pack/{Uri.EscapeDataString(PackToken)}";

    protected override async Task OnInitializedAsync()
    {
        var isAForm = Particulars is not null && Form is not null;
        if (isAForm) await OpenAsync();
    }

    private async Task OpenAsync()
    {
        openProblem = null;
        var answer = await PublicFormRequests.OpenAsync(Http, Particulars!.Code, Form!.Slug, InviteToken, PackToken);
        openProblem = answer.Problem;
        view = answer.Value;
        if (view is { Problem: null } opened) draft = await DraftForAsync(opened);
    }

    private async Task<FormDraft> DraftForAsync(PublicFormView opened)
    {
        var kept = await Drafts.ReadAsync(DraftKey);
        var prefills = opened.Invitation?.Prefills ?? new Dictionary<string, string>();
        return kept is null ? FormDraft.Start(prefills) : FormDraft.Resume(kept);
    }

    private Task KeepDraftAsync() => draft is null ? Task.CompletedTask : Drafts.KeepAsync(DraftKey, draft, Form!);

    private async Task SendAsync()
    {
        isSending = true;
        sendProblem = null;
        var sender = new PublicFormSender(Http, Particulars!.Code, Form!, new FormLinkTokens(InviteToken, PackToken));
        var answer = await sender.SendAsync(draft!, pads);
        receipt = answer.Value;
        sendProblem = answer.Problem;
        isSending = false;
        await AfterSendingAsync();
    }

    private async Task AfterSendingAsync()
    {
        if (receipt is null) await KeepDraftAsync();
        if (receipt is null) return;
        await Drafts.ForgetAsync(DraftKey);
        await JS.InvokeVoidAsync("scrollTo", 0, 0);
    }
}
