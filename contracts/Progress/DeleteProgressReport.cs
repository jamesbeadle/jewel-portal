using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Permanently deletes a progress report and its selections. The underlying progress
/// updates and photos are untouched. Cannot be undone.</summary>
public sealed record DeleteProgressReport(string ProgressReportId) : ICommand<Acknowledgement>;
