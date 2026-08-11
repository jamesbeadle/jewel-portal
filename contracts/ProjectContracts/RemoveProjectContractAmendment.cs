using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.ProjectContracts;

/// <summary>
/// Permanently removes an amendment and its stored document. For the wrong-file-uploaded case;
/// restricted to the same narrow role set that manages the contract terms. This is a hard delete
/// and cannot be undone.
/// </summary>
public sealed record RemoveProjectContractAmendment(
    string ProjectId,
    string ProjectContractAmendmentId) : ICommand<Acknowledgement>;
