using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Edits a progress update's title, description, work date and weather conditions.
/// Photos are managed separately (<see cref="AddProgressPhotos"/> /
/// <see cref="DeleteProgressPhoto"/>).</summary>
public sealed record UpdateProgressUpdate(
    string ProgressUpdateId,
    string Title,
    string Description,
    DateTimeOffset? WorkDate,
    ProgressWeather? Weather) : ICommand<ProgressUpdate>;
