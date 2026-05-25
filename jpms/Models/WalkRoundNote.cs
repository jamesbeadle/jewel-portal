namespace Jewel.JPMS.Models;

public sealed record WalkRoundNote(
    string WalkRoundNoteId,
    string ProjectId,
    string AuthorEmail,
    string Notes,
    int PhotoCount,
    DateTimeOffset CapturedAt);
