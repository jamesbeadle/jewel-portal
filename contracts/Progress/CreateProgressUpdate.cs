using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// Creates a progress update from its words alone — a dated site note on the project's progress
/// feed, with no photographs yet. Photographs follow through <see cref="AddProgressPhotos"/>; the
/// Progress page's own form, which uploads its photographs in the same request, sends
/// <see cref="CreateProgressUpdateWithPhotos"/> instead. Two updates on the same day are two
/// updates (a morning and an afternoon note), never merged.
/// </summary>
public sealed record CreateProgressUpdate(
    string ProjectId,
    string Title,
    string Description,
    DateTimeOffset WorkDate,
    ProgressWeather? Weather,
    string CreatedByEmail) : ICommand<ProgressUpdate>;
