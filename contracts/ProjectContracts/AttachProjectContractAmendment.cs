using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ProjectContracts;

/// <summary>
/// Records a contract amendment against a project after its document has been stored.
///
/// <para>Constructed SERVER-SIDE by <c>UploadProjectContractAmendmentEndpoint</c> once the upload
/// has succeeded — never sent by a client, so it has no client route registration. Same convention
/// as <c>AttachProjectContractDocument</c>.</para>
///
/// <para>Always creates a new row: amendments accumulate, they never replace each other. The
/// document is mandatory — an amendment with nothing signed behind it is a note, not a record.</para>
/// </summary>
public sealed record AttachProjectContractAmendment(
    string ProjectId,
    string ProjectContractAmendmentId,
    string BlobRef,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Title,
    DateTimeOffset? AmendmentDate,
    string? Notes,
    string UploadedByEmail) : ICommand<ProjectContractAmendment>;
