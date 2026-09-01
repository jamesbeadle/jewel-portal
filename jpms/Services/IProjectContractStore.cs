using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Services;

public interface IProjectContractStore
{
    event Action? OnChange;

    /// <summary>True once a fetch for this project has landed — gate figures on this rather than on
    /// <see cref="ForProject"/> being null, which cannot tell "not fetched yet" from "no contract
    /// recorded". Covers the amendments too: one fetch brings both.</summary>
    bool LoadedFor(string projectId);

    /// <summary>The project's contract, or null when none is recorded (or none fetched yet).</summary>
    ProjectContract? ForProject(string projectId);

    /// <summary>The project's amendments in the order they were made. Null until fetched — an empty
    /// list is a real answer ("none recorded"), null is not-fetched-yet.</summary>
    IReadOnlyList<ProjectContractAmendment>? AmendmentsFor(string projectId);

    /// <summary>Fetches at most once per project. Call from OnInitializedAsync, never from render.</summary>
    Task RefreshAsync(string projectId, CancellationToken cancellationToken);

    Task SetTermsAsync(SetProjectContractTerms terms, CancellationToken cancellationToken);

    /// <summary>Uploads the executed contract. Multipart — bypasses the command sender.</summary>
    Task UploadDocumentAsync(string projectId, IBrowserFile file, CancellationToken cancellationToken);

    /// <summary>Uploads a contract amendment's document and records it. Multipart — bypasses the
    /// command sender, same as the executed contract.</summary>
    Task UploadAmendmentAsync(
        string projectId, IBrowserFile file, string title, DateTimeOffset? amendmentDate, string? notes,
        CancellationToken cancellationToken);

    Task SetAmendmentDetailsAsync(SetProjectContractAmendmentDetails details, CancellationToken cancellationToken);

    Task RemoveAmendmentAsync(string projectId, string amendmentId, CancellationToken cancellationToken);
}
