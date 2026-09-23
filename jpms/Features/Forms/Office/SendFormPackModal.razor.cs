using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// Issuing a new starter's pack: who, for which company, how they are engaged, and the four answers
/// the portal decides the forms from — the list is shown before sending, so nobody has to remember
/// which forms a self-employed labourer with a van owes.
/// </summary>
public partial class SendFormPackModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<SentLinkOutcome> OnSent { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private readonly FormWrite write = new();
    private JewelCompany? company;
    private string personName = "";
    private string email = "";
    private Engagement? engagedAs;
    private bool hasP45;
    private bool isWorkingAtAScreen;
    private bool isGettingAVehicle;
    private bool mustHoldATicket;

    private bool IsPlanned => company is not null && engagedAs is not null;

    private bool IsReady => IsPlanned && personName.Trim().Length > 0 && email.Trim().Length > 0;

    private FormPackAnswers Answers =>
        new(engagedAs == Engagement.Employee && hasP45, isWorkingAtAScreen, isGettingAVehicle, mustHoldATicket);

    private IEnumerable<string> PlannedTitles =>
        FormPackPlanner.FormsFor(engagedAs!.Value, Answers).Select(slug => FormCatalogue.TitleOf(slug, company!.Value));

    private async Task SendAsync()
    {
        if (company is not { } chosenCompany || engagedAs is not { } engagement) return;
        SentFormPack? sent = null;
        var command = new SendFormPack(chosenCompany, personName.Trim(), email.Trim(), engagement, Answers);
        var isSent = await write.RunAsync(async () => sent = await Commands.SendAsync(command, CancellationToken.None));
        if (!isSent || sent is null) return;
        personName = "";
        email = "";
        await OnSent.InvokeAsync(SentLinkOutcome.Of(sent));
    }
}
