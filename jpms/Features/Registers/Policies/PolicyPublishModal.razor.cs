using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Features.Registers.Policies;

/// <summary>
/// Publishing a policy revision: its title, summary and declaration, the PDF people read, and any
/// portal users to sign on their own login. The revision is published first and its PDF attached to
/// it; a PDF that does not attach leaves the revision published, to be attached from its row.
/// </summary>
public partial class PolicyPublishModal
{
    private static readonly char[] RecipientSeparators = { '\n', '\r', ',', ';' };

    [Inject] private ICommandSender Commands { get; set; } = default!;
    [Inject] private PolicyFileUpload Files { get; set; } = default!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    /// <summary>Raised once the revision is published, with a sentence when its PDF did not attach.</summary>
    [Parameter] public EventCallback<string?> OnPublished { get; set; }

    private string title = "";
    private string summary = "";
    private string declaration = "";
    private string recipients = "";
    private IBrowserFile? file;
    private bool isPublishing;
    private string? problem;

    private List<string> Recipients =>
        recipients.Split(RecipientSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(email => email.Trim()).Where(email => email.Length > 0).ToList();

    private async Task PublishAsync()
    {
        problem = string.IsNullOrWhiteSpace(title) ? "Give the document a title." : null;
        if (problem is not null) return;
        isPublishing = true;
        try
        {
            var command = new PublishPolicyDocument(title.Trim(), summary, Recipients, declaration.Trim());
            var published = await Commands.SendAsync(command, CancellationToken.None);
            var fileProblem = await AttachFileAsync(published);
            Forget();
            await OnPublished.InvokeAsync(fileProblem);
        }
        catch (CommandFailedException refusal) { problem = refusal.Message; }
        finally { isPublishing = false; }
    }

    private async Task<string?> AttachFileAsync(PolicyDocument published)
    {
        if (file is null) return null;
        try
        {
            await Files.AttachAsync(published.PolicyDocumentId, file, CancellationToken.None);
            return null;
        }
        catch (Exception refusal) when (refusal is InvalidOperationException or HttpRequestException or IOException)
        {
            return $"Published, but the PDF did not attach ({refusal.Message}) — attach it from the revision's row.";
        }
    }

    private void Forget()
    {
        title = "";
        summary = "";
        declaration = "";
        recipients = "";
        file = null;
    }
}
