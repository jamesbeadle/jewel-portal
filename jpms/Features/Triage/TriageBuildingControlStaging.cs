using Jewel.JPMS.Contracts.BuildingControl;

namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// The inspection drafted by "Raise Building Control Inspection" in System Actions: the
/// inspector's email — a booking confirmation, a visit arrangement — becomes an inspection stage
/// on the project's building control case, with the email tagged to it (JPMS/BCI-####) so the
/// stage reads its thread back live. Dates are held as the input's own text ("yyyy-MM-dd") until
/// apply, so a half-typed value never throws — the StagedRecordCreate arrangement. The project
/// must already carry a building control case (set up on its Building Control tab); the server
/// refuses otherwise, with that answer in the red bar.
/// </summary>
public sealed class StagedBuildingControlInspectionDraft
{
    public string StageName { get; set; } = "";
    // No date is read out of the email body: nothing extracts dates from prose anywhere, and the
    // triager just read the email. Blank = Planned; a date = the stage arrives Booked.
    public string BookedFor { get; set; } = "";
    public string InspectorName { get; set; } = "";
    public string OutcomeNotes { get; set; } = "";

    /// <summary>What still stops the inspection being raised — null when it is complete. Shared
    /// by the editor (inline hint) and the page's Apply (hard gate), so the wording is decided
    /// once — the same "decision not yet made" rule as the staged defect and calendar event.</summary>
    public string? Problem
    {
        get
        {
            if (string.IsNullOrWhiteSpace(StageName)) return "Name the inspection stage.";
            if (!string.IsNullOrWhiteSpace(BookedFor) && ParsedBookedFor is null) return "The booked date isn't a date.";
            return null;
        }
    }

    public string Outcome => "raise the inspection on the project's building control case and tag this email to it";

    public DateTime? ParsedBookedFor => ParseDate(BookedFor);

    public CreateBuildingControlInspectionFromMessage ToCommand(
        string messageId, string? internetMessageId, string projectId, LinkThreadScope scope, bool allowCrossPathway)
    {
        return new CreateBuildingControlInspectionFromMessage(
            messageId,
            internetMessageId,
            projectId,
            new BuildingControlInspectionDetails(
                StageName.Trim(),
                ParsedBookedFor is { } booked ? new DateTimeOffset(booked, TimeSpan.Zero) : null,
                InspectedAt: null,
                OutcomeNotes.Trim(),
                InspectorName.Trim()),
            Scope: scope,
            AllowCrossPathway: allowCrossPathway);
    }

    private static DateTime? ParseDate(string text) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var value)
            ? value
            : null;
}
