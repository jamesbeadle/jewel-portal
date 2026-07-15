using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Permanently deletes a progress update, its photos and their stored files, and removes
/// it from any report selections. Cannot be undone.</summary>
public sealed record DeleteProgressUpdate(string ProgressUpdateId) : ICommand<Acknowledgement>;
