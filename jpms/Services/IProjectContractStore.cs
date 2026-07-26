using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

public interface IProjectContractStore
{
    event Action? OnChange;

    /// <summary>True once a fetch for this project has landed — gate figures on this rather than on
    /// <see cref="ForProject"/> being null, which cannot tell "not fetched yet" from "no contract
    /// recorded".</summary>
    bool LoadedFor(string projectId);

    /// <summary>The project's contract, or null when none is recorded (or none fetched yet).</summary>
    ProjectContract? ForProject(string projectId);

    /// <summary>Fetches at most once per project. Call from OnInitializedAsync, never from render.</summary>
    Task RefreshAsync(string projectId, CancellationToken cancellationToken);

    Task SetTermsAsync(SetProjectContractTerms terms, CancellationToken cancellationToken);

    /// <summary>Uploads the executed contract. Multipart — bypasses the command sender.</summary>
    Task UploadDocumentAsync(string projectId, IBrowserFile file, CancellationToken cancellationToken);
}
