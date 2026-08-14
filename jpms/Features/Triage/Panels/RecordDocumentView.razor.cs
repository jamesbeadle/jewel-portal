using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Features.Triage.Workspace;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Features.Triage.Panels;

public partial class RecordDocumentView
{
    [Parameter, EditorRequired] public LinkableRecord Record { get; set; } = default!;
    [Parameter] public EventCallback OnBack { get; set; }
    [Parameter] public EventCallback<PreviewRequest> OnPreview { get; set; }

    /// <summary>Start a reply to one of the record's emails — passed through to the
    /// correspondence list; the host decides what replying means.</summary>
    [Parameter] public EventCallback<MailboxMessage> OnReply { get; set; }

    /// <summary>Start a forward of one of the record's emails — same pass-through as
    /// <see cref="OnReply"/>.</summary>
    [Parameter] public EventCallback<MailboxMessage> OnForward { get; set; }

    // A request is the one type read in full here; every other type shows its explorer summary.
    private Request? request;
    private bool requestLoading;
    private string loadedRequestId = "";

    private string? StatusLabel => request?.Status.DisplayName() ?? Record.StatusLabel;

    protected override async Task OnParametersSetAsync()
    {
        if (Record.Type != RecordType.Request || Record.RecordId == loadedRequestId) return;
        loadedRequestId = Record.RecordId;
        request = null;
        requestLoading = true;
        try
        {
            request = await Queries.AskAsync(new GetRequestById(Record.RecordId), CancellationToken.None);
        }
        catch
        {
            // The summary view below still stands; the query client has reported the failure.
        }
        finally
        {
            requestLoading = false;
        }
    }

    private static string DateText(DateTimeOffset? value) =>
        value is { } date ? date.LocalDateTime.ToString("d MMM yyyy") : "—";
}
