using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ProjectContracts;

/// <summary>
/// Records the executed contract document against a project after the blob has been stored.
///
/// <para>Constructed SERVER-SIDE by <c>UploadProjectContractDocumentEndpoint</c> once the upload has
/// succeeded — never sent by a client, so it has no client route registration. Same convention as
/// <c>AddComplianceDocumentVersion</c>.</para>
///
/// <para>Creates the contract row if the terms have not been entered yet, so a user can upload the
/// PDF first and fill the terms in afterwards. The previous blob ref is returned to the caller for
/// best-effort cleanup.</para>
/// </summary>
public sealed record AttachProjectContractDocument(
    string ProjectId,
    string BlobRef,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string UploadedByEmail) : ICommand<ProjectContract>;
