using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Merges two General requests into one, before either has been promoted to an RFI. The survivor
/// keeps its reference/title; the other request's description folds in beneath it, its conversation
/// history and itemised queries move across, and its mailbox emails are retagged to the survivor so
/// the live-read correspondence follows. The merged-away request is closed and stamped with
/// MergedIntoRequestId — kept for the audit trail, never counted as open again.
/// </summary>
public sealed record MergeRequests(string SurvivorRequestId, string MergedRequestId) : ICommand<Request>;
