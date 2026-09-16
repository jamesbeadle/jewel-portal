using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// Writes the days a person confirmed: one progress update per day, through the same commands the
/// Progress page uses — with its photographs in one save when any could be stored, from its words
/// alone otherwise. A day that already holds an update is refused, by name, before anything is
/// written, so re-running a week never duplicates it.
/// </summary>
public sealed class WhatsAppWeekWriter
{
    private readonly ProgressPhotoIntake intake;
    private readonly CreateProgressUpdateWithPhotosValidation withPhotosValidation;
    private readonly ICommandHandler<CreateProgressUpdateWithPhotos, ProgressUpdate> withPhotos;
    private readonly CreateProgressUpdateValidation wordsOnlyValidation;
    private readonly ICommandHandler<CreateProgressUpdate, ProgressUpdate> wordsOnly;

    public WhatsAppWeekWriter(
        ProgressPhotoIntake intake,
        CreateProgressUpdateWithPhotosValidation withPhotosValidation,
        ICommandHandler<CreateProgressUpdateWithPhotos, ProgressUpdate> withPhotos,
        CreateProgressUpdateValidation wordsOnlyValidation,
        ICommandHandler<CreateProgressUpdate, ProgressUpdate> wordsOnly)
    {
        this.intake = intake;
        this.withPhotosValidation = withPhotosValidation;
        this.withPhotos = withPhotos;
        this.wordsOnlyValidation = wordsOnlyValidation;
        this.wordsOnly = wordsOnly;
    }

    /// <summary>Why the week cannot be written as asked, or null when it can.</summary>
    public static string? Refusal(WhatsAppWeekReading reading, IReadOnlyList<DateOnly> days)
    {
        if (days.Count == 0) return "No days were chosen.";
        var unknown = days.Where(day => reading.Plan.Days.All(planned => planned.Date != day)).ToList();
        if (unknown.Count > 0) return $"No messages for this project on {Named(unknown)}.";
        var taken = days.Where(day => reading.ExistingOn(day).Count > 0).ToList();
        if (taken.Count > 0) return $"An update already exists on {Named(taken)} — untick those days, or delete the existing updates first.";
        return null;
    }

    public async Task<WhatsAppWeekApplied> WriteAsync(
        string projectId, WhatsAppWeekReading reading, IReadOnlyList<DateOnly> days, string createdByEmail, CancellationToken cancellationToken)
    {
        var written = new List<WhatsAppWeekDayWritten>();
        foreach (var day in reading.Plan.Days.Where(planned => days.Contains(planned.Date)))
        {
            written.Add(await WriteDayAsync(projectId, day, reading.Photos.On(day.Date), createdByEmail, cancellationToken));
        }
        return new WhatsAppWeekApplied(written);
    }

    private async Task<WhatsAppWeekDayWritten> WriteDayAsync(
        string projectId, WhatsAppPlannedDay day, IReadOnlyList<IncomingProgressPhoto> photos, string createdByEmail, CancellationToken cancellationToken)
    {
        var words = new DayWords(day.Date, WhatsAppDayText.Title(day.Date), WhatsAppDayText.Description(day.Messages));
        var updateId = ProgressIdentifierFactory.NextProgressUpdateId();
        var taken = photos.Count == 0 ? null : await intake.TakeAsync(projectId, updateId, photos, cancellationToken);
        var outcomes = taken?.Outcomes ?? Array.Empty<ProgressPhotoIntakeOutcome>();

        if (taken is null || taken.Stored.Count == 0)
            return await WriteWordsAsync(projectId, words, outcomes, createdByEmail, cancellationToken);

        var command = new CreateProgressUpdateWithPhotos(
            updateId, projectId, words.Title, words.Description, words.WorkDate, null, createdByEmail, taken.Stored);
        Refuse(withPhotosValidation.Check(command), day.Date);
        var created = await withPhotos.HandleAsync(command, cancellationToken);
        return new WhatsAppWeekDayWritten(day.Date, created.ProgressUpdateId, words.Title, outcomes);
    }

    private async Task<WhatsAppWeekDayWritten> WriteWordsAsync(
        string projectId, DayWords words, IReadOnlyList<ProgressPhotoIntakeOutcome> outcomes, string createdByEmail, CancellationToken cancellationToken)
    {
        var command = new CreateProgressUpdate(projectId, words.Title, words.Description, words.WorkDate, null, createdByEmail);
        Refuse(wordsOnlyValidation.Check(command), words.Date);
        var created = await wordsOnly.HandleAsync(command, cancellationToken);
        return new WhatsAppWeekDayWritten(words.Date, created.ProgressUpdateId, words.Title, outcomes);
    }

    private sealed record DayWords(DateOnly Date, string Title, string Description)
    {
        public DateTimeOffset WorkDate => new(Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private static void Refuse(ValidationOutcome outcome, DateOnly day)
    {
        if (outcome.HasFailed)
            throw new InvalidOperationException($"{day:d MMMM}: {string.Join(" ", outcome.Errors)}");
    }

    private static string Named(IEnumerable<DateOnly> days) =>
        string.Join(", ", days.OrderBy(day => day).Select(day => day.ToString("dddd d MMMM")));
}
