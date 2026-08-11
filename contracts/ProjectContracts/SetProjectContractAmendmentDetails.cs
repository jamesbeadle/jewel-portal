using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ProjectContracts;

/// <summary>
/// Corrects the title, date or notes on a recorded amendment. Deliberately does not touch the
/// document — a wrong file is fixed by removing the amendment and uploading again, so re-wording a
/// title can never detach the signed deed.
///
/// <para><c>UpdatedByEmail</c> is re-stamped from the session by the endpoint; whatever a client
/// sends is discarded.</para>
/// </summary>
public sealed record SetProjectContractAmendmentDetails(
    string ProjectId,
    string ProjectContractAmendmentId,
    string UpdatedByEmail,
    string Title,
    DateTimeOffset? AmendmentDate,
    string? Notes) : ICommand<ProjectContractAmendment>;
