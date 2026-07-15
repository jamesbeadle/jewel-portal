using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Edits a progress update's title, description and work date. Photos are managed
/// separately (<see cref="AddProgressPhotos"/> / <see cref="DeleteProgressPhoto"/>).</summary>
public sealed record UpdateProgressUpdate(
    string ProgressUpdateId,
    string Title,
    string Description,
    DateTimeOffset? WorkDate) : ICommand<ProgressUpdate>;
