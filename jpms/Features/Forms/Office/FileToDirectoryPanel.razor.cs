using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// A questionnaire's or an insurance update's certificates onto a directory company's compliance
/// record (the dashboard's op:'formrenewal'), each with the expiry the form collected, so the renewal
/// is chased and the £5m check reads them like any other certificate. The company is matched on the
/// name the form gave and confirmed by a person; nothing is filed to a company picked by a guess.
/// </summary>
public partial class FileToDirectoryPanel
{
    [Parameter, EditorRequired] public FormSubmissionReading Reading { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }

    private readonly FormWrite write = new();
    private List<DirectoryFilingRow> rows = new();
    private string subcontractorId = "";
    private bool isOpen;
    private FormDirectoryFiling? filed;

    private string Lead => Reading.Submission.FormSlug == FormSlugs.InsuranceUpdate
        ? "An insurance update. File the certificate to the company's directory record, with its expiry, and the form is handled."
        : "A sub-contractor questionnaire. File its insurance certificates to the company's directory record, with their expiry, "
            + "and the renewal is chased like any other certificate.";

    private IReadOnlyList<SearchSelect.Option> Companies => Directory.All()
        .OrderBy(company => company.CompanyName)
        .Select(company => new SearchSelect.Option(company.SubcontractorId, company.CompanyName))
        .ToList();

    private bool IsReady => subcontractorId.Length > 0 && rows.Any(row => row.IsIncluded);

    protected override void OnInitialized() => Directory.OnChange += StateHasChanged;

    private void Open()
    {
        rows = FormDirectoryFilingPlan.For(Reading.View).Select(suggestion => new DirectoryFilingRow(suggestion)).ToList();
        var formCompany = Reading.View.Answers.GetValueOrDefault("company", "").Trim();
        var match = Directory.All()
            .FirstOrDefault(company => string.Equals(company.CompanyName.Trim(), formCompany, StringComparison.OrdinalIgnoreCase));
        subcontractorId = match?.SubcontractorId ?? "";
        isOpen = true;
    }

    private async Task FileAsync()
    {
        var files = rows.Where(row => row.IsIncluded).Select(row => row.ToFile()).ToList();
        var command = new FileFormToDirectory(Reading.Submission.FormSubmissionId, subcontractorId, files);
        var isFiled = await write.RunAsync(async () => filed = await Commands.SendAsync(command, CancellationToken.None));
        isOpen = !isFiled;
        if (isFiled) await OnChanged.InvokeAsync();
    }

    public void Dispose() => Directory.OnChange -= StateHasChanged;
}
