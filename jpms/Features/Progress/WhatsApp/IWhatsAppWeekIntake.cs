using Jewel.JPMS.Contracts.Progress;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Features.Progress.WhatsApp;

/// <summary>The week of the site WhatsApp group brought into the portal as progress updates:
/// read first, then written only for the days a person ticked on the review screen.</summary>
public interface IWhatsAppWeekIntake
{
    /// <summary>Reads the week and answers the review screen. Writes nothing.</summary>
    Task<WhatsAppWeekPreview> PreviewAsync(
        string projectId, DateOnly weekEnding, string chatText, IBrowserFile? export, CancellationToken cancellationToken);

    /// <summary>Writes one progress update per chosen day, with that day's photographs.</summary>
    Task<WhatsAppWeekApplied> ApplyAsync(
        string projectId, DateOnly weekEnding, string chatText, IBrowserFile? export,
        IReadOnlyList<DateOnly> days, CancellationToken cancellationToken);
}
